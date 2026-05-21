using System;
using System.Collections.Generic;
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
    // Definiujemy zdarzenie dla MainWindow
    public event EventHandler? BackClicked;

    private readonly List<SerialPort> _espPorts = new();

    public ConfigureWifiView()
    {
        InitializeComponent();
        
        // Przekazanie sygnału cofania do góry po kliknięciu przycisku
        BackButton.Click += (sender, e) =>
        {
            BackClicked?.Invoke(this, EventArgs.Empty);
        };

        DetachedFromVisualTree += (s, e) => 
        {
            foreach (var port in _espPorts)
            {
                port.Close();
                port.Dispose();
            }
            _espPorts.Clear();
        };
    }

    private async void ScanButton_Click(object? sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = "Status: Scanning all ports for ESP devices...";
        StatusTextBlock.Foreground = Brushes.Orange;
        InputPanel.IsEnabled = false;

        foreach (var port in _espPorts)
        {
            port.Close();
            port.Dispose();
        }
        _espPorts.Clear();

        var foundPorts = await Task.Run(ScanForAllEsps);
        _espPorts.AddRange(foundPorts);

        if (_espPorts.Count > 0)
        {
            var portNames = string.Join(", ", _espPorts.Select(p => p.PortName));
            StatusTextBlock.Text = $"Status: Found {_espPorts.Count} ESP(s) on {portNames}. Ready to configure.";
            StatusTextBlock.Foreground = Brushes.Green;
            InputPanel.IsEnabled = true;
            MqttIpTextBox.Text = GetLocalIPAddress();
        }
        else
        {
            StatusTextBlock.Text = "Status: No ESP devices found. Try again.";
            StatusTextBlock.Foreground = Brushes.Red;
        }
    }

    private List<SerialPort> ScanForAllEsps()
    {
        var foundPorts = new List<SerialPort>();
        string[] portNames = SerialPort.GetPortNames();
        Debug.WriteLine($"\n[C#] Found system ports: {string.Join(", ", portNames)}");

        foreach (var portName in portNames)
        {
            try
            {
                var sp = new SerialPort(portName, 115200) { WriteTimeout = 1000, ReadTimeout = 1000 };
                sp.DtrEnable = true;
                sp.RtsEnable = true;
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
                        sp.Write("PING\n");
                        lastPingTime = DateTime.Now;
                    }

                    if (sp.IsOpen && sp.BytesToRead > 0)
                    {
                        buffer += sp.ReadExisting();
                        if (buffer.Contains("START_APLIKACJA"))
                        {
                            Debug.WriteLine($"[C#] Handshake successful on {portName}. Adding to list.");
                            foundPorts.Add(sp);
                            goto NextPort;
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
            NextPort:;
        }
        return foundPorts;
    }

    private void ChangeWifiButton_Click(object? sender, RoutedEventArgs e)
    {
        InputPanel.IsEnabled = true;
        PasswordTextBox.Text = "";
        StatusTextBlock.Text = "Status: Ready to configure new WiFi. Waking up ESP32s...";
        StatusTextBlock.Foreground = Brushes.LightBlue;
        ScanButton_Click(null, new RoutedEventArgs());
    }
    private async void DisconnectButton_Click(object? sender, RoutedEventArgs e)
{
    StatusTextBlock.Text = "Status: Disconnecting ESP devices from network...";
    StatusTextBlock.Foreground = Brushes.Orange;
    InputPanel.IsEnabled = false;

    // 1. Wysyłamy komendę do urządzeń
    foreach (var port in _espPorts)
    {
        if (port != null && port.IsOpen)
        {
            try
            {
                port.DtrEnable = true;
                port.RtsEnable = true;
                
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                
                port.Write("DISCONNECT_CMD\n");
                Debug.WriteLine($"[C#] Sent DISCONNECT_CMD to {port.PortName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[C#] Failed to send command to {port.PortName}: {ex.Message}");
            }
        }
    }

    // 2. KLUCZOWA POPRAWKA: Dajemy systemowi Windows pełne 1.5 sekundy (1500ms)
    // na to, aby ESP32-C3 odebrało komendę, wyczyściło pamięć i wykonało ESP.restart().
    // W tym czasie Windows usłyszy restart urządzenia i odblokuje sterownik portu COM.
    await Task.Delay(1500);

    // 3. Zamykamy i bezwzględnie niszczymy obiekty w C#
    try
    {
        foreach (var port in _espPorts)
        {
            try
            {
                if (port.IsOpen) 
                {
                    port.DiscardInBuffer();
                    port.DiscardOutBuffer();
                    port.Close();
                }
                port.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[C#] Soft close warning on {port.PortName}: {ex.Message}");
            }
        }
        _espPorts.Clear(); // Czyszczenie listy
        
        StatusTextBlock.Text = "Status: Disconnected. Ports are fully unlocked and ready.";
        StatusTextBlock.Foreground = Brushes.Crimson;
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"[C#] Error during final hardware port clear: {ex.Message}");
        StatusTextBlock.Text = "Status: Error while freeing hardware ports.";
        StatusTextBlock.Foreground = Brushes.Red;
    }
}
    
    private async void ConnectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_espPorts.Count == 0)
        {
            StatusTextBlock.Text = "Status: No ESPs found. Please scan again.";
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

        StatusTextBlock.Text = $"Status: Configuring {_espPorts.Count} devices...";
        StatusTextBlock.Foreground = Brushes.Orange;
        InputPanel.IsEnabled = false;

        int successCount = 0;
        var tasks = _espPorts.Select(port => ConfigureDeviceAsync(port, ssid, password, mqttIp));
        var results = await Task.WhenAll(tasks);

        foreach (var (portName, success) in results)
        {
            if (success)
            {
                Debug.WriteLine($"[C#] Successfully configured {portName}.");
                successCount++;
            }
            else
            {
                Debug.WriteLine($"[C#] FAILED to configure {portName}.");
            }
        }

        StatusTextBlock.Text = $"Configuration complete. Successfully configured {successCount} out of {_espPorts.Count} devices.";
        StatusTextBlock.Foreground = successCount == _espPorts.Count ? Brushes.Green : Brushes.OrangeRed;
    }

    private async Task<(string portName, bool success)> ConfigureDeviceAsync(SerialPort port, string ssid, string password, string mqttIp)
    {
        var wifiResult = await SendCommandAsync(port, $"WIFI_CONFIG:{ssid}:{password}\n", "WIFI_CONFIRMED", "WIFI_FAILED");
        if (wifiResult != "SUCCESS")
        {
            return (port.PortName, false);
        }
        
        var mqttResult = await SendCommandAsync(port, $"MQTT_CONFIG:{mqttIp}\n", "MQTT_CONFIRMED", "MQTT_FAILED");
        return (port.PortName, mqttResult == "SUCCESS");
    }

    private async Task<string> SendCommandAsync(SerialPort port, string command, string successResponse, string failureResponse, int timeoutSeconds = 15)
    {
        if (port == null || !port.IsOpen) return "ERROR: Port closed";

        return await Task.Run(() =>
        {
            var overallTimeout = DateTime.Now.AddSeconds(timeoutSeconds);
            var buffer = "";

            try
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
                port.Write(command);
                Debug.WriteLine($"[C#] SENT to {port.PortName}: {command.Trim()}");
            }
            catch (Exception ex) { return $"ERROR: Write failed ({ex.Message})"; }

            while (DateTime.Now < overallTimeout)
            {
                try
                {
                    if (port.IsOpen && port.BytesToRead > 0)
                    {
                        buffer += port.ReadExisting();
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
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .Select(ni => ni.GetIPProperties())
                .SelectMany(ni => ni.UnicastAddresses)
                .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip.Address))
                .Select(ip => ip.Address.ToString())
                .FirstOrDefault("192.168.1.100");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[C#] Could not get local IP: {ex.Message}");
            return "192.168.1.100";
        }
    }
}