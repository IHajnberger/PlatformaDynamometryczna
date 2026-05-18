using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace UI_Test_Avalonia;

public partial class ConfigureWifiView : UserControl
{
    private SerialPort? _espPort;
    private CancellationTokenSource? _debugReaderCts;

    public ConfigureWifiView()
    {
        InitializeComponent();
        
        DetachedFromVisualTree += (s, e) => 
        {
            _debugReaderCts?.Cancel();
            _espPort?.Close();
        };
    }

    private async void ScanButton_Click(object? sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = "Status: Scanning ports...";
        StatusTextBlock.Foreground = Brushes.Orange;
        InputPanel.IsEnabled = false;

        _debugReaderCts?.Cancel();
        if (_espPort != null && _espPort.IsOpen)
        {
            _espPort.Close();
            _espPort.Dispose();
        }

        _espPort = await Task.Run(ScanForEsp);

        if (_espPort != null)
        {
            StatusTextBlock.Text = $"Status: ESP confirmed on {_espPort.PortName}. Ready.";
            StatusTextBlock.Foreground = Brushes.Green;
            InputPanel.IsEnabled = true;
            
            // Auto-fill the IP address
            MqttIpTextBox.Text = GetLocalIPAddress();
            
            _debugReaderCts = new CancellationTokenSource();
            _ = Task.Run(() => ReadSerialData(_debugReaderCts.Token));
        }
        else
        {
            StatusTextBlock.Text = "Status: ESP not found. Try again.";
            StatusTextBlock.Foreground = Brushes.Red;
        }
    }

    private SerialPort? ScanForEsp()
    {
        string[] ports = SerialPort.GetPortNames();
        Debug.WriteLine($"\n[C#] Found ports: {string.Join(", ", ports)}");

        foreach (var portName in ports)
            try
            {
                var sp = new SerialPort(portName, 115200) { WriteTimeout = 1000, ReadTimeout = 1000 };
                sp.DtrEnable = false;
                sp.RtsEnable = false;
                sp.Open();

                Thread.Sleep(1500);
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();

                var timeout = DateTime.Now.AddSeconds(3.0);
                var buffer = "";
                var lastPingTime = DateTime.MinValue;

                while (DateTime.Now < timeout)
                {
                    if ((DateTime.Now - lastPingTime).TotalMilliseconds > 500)
                    {
                        sp.Write("\nPING\n");
                        lastPingTime = DateTime.Now;
                    }

                    if (sp.IsOpen && sp.BytesToRead > 0)
                    {
                        buffer += sp.ReadExisting();
                        if (buffer.Contains("START_APLIKACJA"))
                        {
                            Debug.WriteLine($"[C#] Handshake successful on {portName}");
                            return sp;
                        }
                    }
                    Thread.Sleep(50);
                }
                sp.Close();
                sp.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[C#] Error probing {portName}: {ex.Message}");
            }

        return null;
    }
    
    private async Task ReadSerialData(CancellationToken token)
    {
        Debug.WriteLine($"[Debug Reader] Starting background reader for {_espPort?.PortName}.");
        while (!token.IsCancellationRequested && _espPort != null && _espPort.IsOpen)
        {
            try
            {
                if (_espPort.BytesToRead > 0)
                {
                    var data = _espPort.ReadExisting();
                    Debug.Write($"[ESP32] {data}");
                }
            }
            catch (Exception) { break; }
            await Task.Delay(50, token);
        }
        Debug.WriteLine("[Debug Reader] Background reader stopped.");
    }

    private void ChangeWifiButton_Click(object? sender, RoutedEventArgs e)
    {
        InputPanel.IsEnabled = true;
        PasswordTextBox.Text = "";
        StatusTextBlock.Text = "Status: Ready to configure new WiFi. Waking up ESP32...";
        StatusTextBlock.Foreground = Brushes.LightBlue;
        ScanButton_Click(null, new RoutedEventArgs());
    }

    private async void ConnectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_espPort == null || !_espPort.IsOpen)
        {
            StatusTextBlock.Text = "Status: Port is closed. Please scan again.";
            StatusTextBlock.Foreground = Brushes.Red;
            return;
        }

        var ssid = SsidTextBox.Text ?? "";
        var password = PasswordTextBox.Text ?? "";
        var mqttIp = MqttIpTextBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(ssid) || string.IsNullOrWhiteSpace(mqttIp))
        {
            StatusTextBlock.Text = "Status: SSID and MQTT IP cannot be empty.";
            StatusTextBlock.Foreground = Brushes.Red;
            return;
        }

        StatusTextBlock.Text = "Status: Sending credentials...";
        StatusTextBlock.Foreground = Brushes.Orange;

        _debugReaderCts?.Cancel();

        var wifiResult = await SendCommandAsync($"WIFI_CONFIG:{ssid}:{password}\n", "WIFI_CONFIRMED", "WIFI_FAILED");
        
        if (wifiResult == "SUCCESS")
        {
            StatusTextBlock.Text = "Status: WiFi confirmed. Sending MQTT config...";
            var mqttResult = await SendCommandAsync($"MQTT_CONFIG:{mqttIp}\n", "MQTT_CONFIRMED", "MQTT_FAILED");

            if (mqttResult == "SUCCESS")
            {
                StatusTextBlock.Text = "Status: Full config sent! ESP is restarting.";
                StatusTextBlock.Foreground = Brushes.Green;
                InputPanel.IsEnabled = false;
                _debugReaderCts = new CancellationTokenSource();
                _ = Task.Run(() => ReadSerialData(_debugReaderCts.Token));
            }
            else
            {
                StatusTextBlock.Text = $"Status: MQTT Config Failed ({mqttResult})";
                StatusTextBlock.Foreground = Brushes.Red;
            }
        }
        else
        {
            StatusTextBlock.Text = $"Status: WiFi Connection Failed ({wifiResult})";
            StatusTextBlock.Foreground = Brushes.Red;
            _debugReaderCts = new CancellationTokenSource();
            _ = Task.Run(() => ReadSerialData(_debugReaderCts.Token));
        }
    }

    private async Task<string> SendCommandAsync(string command, string successResponse, string failureResponse, int timeoutSeconds = 15)
    {
        if (_espPort == null || !_espPort.IsOpen) return "ERROR: Port closed";

        return await Task.Run(() =>
        {
            var overallTimeout = DateTime.Now.AddSeconds(timeoutSeconds);
            var buffer = "";

            try
            {
                _espPort.DiscardInBuffer();
                _espPort.DiscardOutBuffer();
                _espPort.Write(command);
                Debug.WriteLine($"[C#] SENT: {command.Trim()}");
            }
            catch (Exception ex) { return $"ERROR: Write failed ({ex.Message})"; }

            while (DateTime.Now < overallTimeout)
            {
                try
                {
                    if (_espPort.IsOpen && _espPort.BytesToRead > 0)
                    {
                        buffer += _espPort.ReadExisting();
                        if (buffer.Contains(successResponse)) return "SUCCESS";
                        if (buffer.Contains(failureResponse)) return "FAILED";
                    }
                }
                catch (Exception) { return "ERROR: Read failed"; }
                Thread.Sleep(50);
            }
            return "TIMEOUT";
        });
    }

    private string GetLocalIPAddress()
    {
        try
        {
            // Get all network interfaces
            return NetworkInterface.GetAllNetworkInterfaces()
                // Filter for ones that are running
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                // Get their IP properties
                .Select(ni => ni.GetIPProperties())
                // Get all of their unicast addresses
                .SelectMany(ni => ni.UnicastAddresses)
                // Filter for IPv4 addresses that are not loopback
                .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                // Select the address itself
                .Select(ip => ip.Address.ToString())
                // Get the first one, or a default value
                .FirstOrDefault("192.168.1.100");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[C#] Could not get local IP: {ex.Message}");
            return "192.168.1.100"; // Fallback
        }
    }
}