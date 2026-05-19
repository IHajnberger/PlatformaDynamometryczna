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
    // Now we store a list of all found ESP ports
    private readonly List<SerialPort> _espPorts = new();

    public ConfigureWifiView()
    {
        InitializeComponent();
        
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

        // Clean up any old ports
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
            
            // Auto-fill the IP address
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
                            Debug.WriteLine($"[C#] Handshake successful on {portName}. Adding to list.");
                            foundPorts.Add(sp);
                            goto NextPort; // Exit the inner while loop and move to the next port
                        }
                    }
                    Thread.Sleep(50);
                }
                
                // If we get here, it wasn't an ESP. Close it.
                sp.Close();
                sp.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[C#] Error probing {portName}: {ex.Message}");
            }
            
            NextPort:; // Label to jump to for the goto statement
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
        
        // Configure all found ports in parallel
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