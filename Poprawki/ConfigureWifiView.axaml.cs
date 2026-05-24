using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace UI_Test_Avalonia;

/*
kod edytowany pod Serial, do poprawy na full mqtt, ale na razie to jest szybkie rozwiązanie do testów
cuz nie działa mi mqtt 
*/
public partial class ConfigureWifiView : UserControl
{
    public event EventHandler? BackClicked;

    private readonly List<SerialPort> _espPorts = [];

    public static ConcurrentQueue<(double Weight, DateTime Timestamp)> Device1Queue { get; } = new();
    public static ConcurrentQueue<(double Weight, DateTime Timestamp)> Device2Queue { get; } = new();
    public static DateTime LastPacketTime { get; private set; } = DateTime.MinValue;

    public ConfigureWifiView()
    {
        InitializeComponent();

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
        foreach (var port in _espPorts)
        {
            StartContinuousReading(port);
        }

        if (_espPorts.Count > 0)
        {
            var portNames = string.Join(", ", _espPorts.Select(p => p.PortName));
            StatusTextBlock.Text = $"Status: Found {_espPorts.Count} ESP(s) on {portNames}. Ready.";
            StatusTextBlock.Foreground = Brushes.Green;
            InputPanel.IsEnabled = true;
        }
        else
        {
            StatusTextBlock.Text = "Status: No ESP devices found. Try again.";
            StatusTextBlock.Foreground = Brushes.Red;
        }
    }
    // dead zone + średnia krocząca do wygładzania danych z wagi, aby uniknąć szumów i drobnych wahań
    private static double _avgLeft = 0;
    private static double _avgRight = 0;
    private const double Alpha = 0.1;
    private const double DeadZone = 0.1;

    private void StartContinuousReading(SerialPort port)
    {
        var thread = new Thread(() =>
        {
            var buffer = "";
            while (true)
            {
                try
                {
                    if (!port.IsOpen)
                    {
                        try { port.Open(); }
                        catch { Thread.Sleep(1000); continue; }
                    }

                    if (port.BytesToRead > 0)
                    {
                        buffer += port.ReadExisting();
                        var lines = buffer.Split('\n');
                        buffer = lines[^1];

                        foreach (var line in lines[..^1])
                        {
                            if (line.StartsWith("DATA:"))
                            {
                                var parts = line.Replace("DATA:", "").Trim().Split(':');
                                if (parts.Length == 2 &&
                                    double.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                                        System.Globalization.CultureInfo.InvariantCulture, out double left) &&
                                    double.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                                        System.Globalization.CultureInfo.InvariantCulture, out double right))
                                {
                                    // Dead zone
                                    if (Math.Abs(left) < DeadZone) left = 0;
                                    if (Math.Abs(right) < DeadZone) right = 0;

                                    // Średnia krocząca
                                    _avgLeft = Alpha * left + (1 - Alpha) * _avgLeft;
                                    _avgRight = Alpha * right + (1 - Alpha) * _avgRight;

                                    Device1Queue.Enqueue((_avgLeft, DateTime.Now));
                                    Device2Queue.Enqueue((_avgRight, DateTime.Now));
                                    LastPacketTime = DateTime.Now;
                                    Debug.WriteLine($"LEFT={_avgLeft:F3} RIGHT={_avgRight:F3}");
                                }
                            }
                        }
                    }
                    Thread.Sleep(20);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ReadThread] Error: {ex.Message}");
                    buffer = "";
                    Thread.Sleep(1000);
                }
            }
        })
        { IsBackground = true };

        thread.Start();
    }

    private List<SerialPort> ScanForAllEsps()
    {
        var foundPorts = new List<SerialPort>();
        string[] portNames = SerialPort.GetPortNames();
        Debug.WriteLine($"\n[C#] Found system ports: {string.Join(", ", portNames)}");

        var tasks = portNames.Select(portName => Task.Run(() =>
        {
            SerialPort? sp = null;
            try
            {
                sp = new SerialPort(portName, 115200)
                {
                    WriteTimeout = 1000,
                    ReadTimeout = 1000,
                    DtrEnable = false,
                    RtsEnable = false
                };

                sp.Open();
                Thread.Sleep(500);
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();

                var timeout = DateTime.Now.AddSeconds(5.0);
                var buffer = "";
                var lastPingTime = DateTime.MinValue;

                while (DateTime.Now < timeout)
                {
                    if ((DateTime.Now - lastPingTime).TotalMilliseconds > 500)
                    {
                        sp.Write("PING\n");
                        lastPingTime = DateTime.Now;
                        Debug.WriteLine($"[C#] PING sent to {portName}");
                    }

                    if (sp.IsOpen && sp.BytesToRead > 0)
                    {
                        buffer += sp.ReadExisting();
                        Debug.WriteLine($"[C#] {portName} received: {buffer}");

                        if (buffer.Contains("START_APLIKACJA") || buffer.Contains("CONFIG LOADED"))
                        {
                            Debug.WriteLine($"[C#] Handshake OK on {portName}");
                            return sp;
                        }
                    }

                    Thread.Sleep(50);
                }

                Debug.WriteLine($"[C#] No response from {portName}, closing.");
                sp.Close();
                sp.Dispose();
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[C#] Error probing {portName}: {ex.Message}");
                sp?.Close();
                sp?.Dispose();
                return null;
            }
        })).ToArray();

        Task.WaitAll(tasks);

        foreach (var task in tasks)
        {
            if (task.Result != null)
                foundPorts.Add(task.Result);
        }

        return foundPorts;
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
        Debug.WriteLine($"[C#] MQTT IP that will be sent: '{mqttIp}'");

        var wifiResult = await SendCommandAsync(port, $"WIFI_CONFIG:{ssid}:{password}\n", "WIFI_CONFIRMED", "WIFI_FAILED");
        if (wifiResult != "SUCCESS")
            return (port.PortName, false);

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

    private async void DisconnectButton_Click(object? sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = "Status: Disconnecting ESP devices from network...";
        StatusTextBlock.Foreground = Brushes.Orange;
        InputPanel.IsEnabled = false;

        foreach (var port in _espPorts)
        {
            if (port != null && port.IsOpen)
            {
                try
                {
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

        await Task.Delay(1500);

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
            _espPorts.Clear();

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

    private void ChangeWifiButton_Click(object? sender, RoutedEventArgs e)
    {
        InputPanel.IsEnabled = true;
        PasswordTextBox.Text = "";
        StatusTextBlock.Text = "Status: Ready to configure new WiFi. Waking up ESP32s...";
        StatusTextBlock.Foreground = Brushes.LightBlue;
        ScanButton_Click(null, new RoutedEventArgs());
    }
}