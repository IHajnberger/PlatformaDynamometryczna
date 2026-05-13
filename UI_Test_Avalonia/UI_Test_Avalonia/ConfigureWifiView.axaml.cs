using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace UI_Test_Avalonia;

public partial class ConfigureWifiView : UserControl
{
    // Store the OPEN connection instead of just the string name
    private SerialPort? _espPort;

    public ConfigureWifiView()
    {
        InitializeComponent();
    }

    private async void ScanButton_Click(object? sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = "Status: Scanning ports...";
        StatusTextBlock.Foreground = Brushes.Orange;
        InputPanel.IsEnabled = false;

        // Clean up any old open ports just in case
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
                // REMOVED 'using' block. We want to keep it alive if it succeeds!
                var sp = new SerialPort(portName, 115200) { WriteTimeout = 1000, ReadTimeout = 1000 };
                sp.DtrEnable = false;
                sp.RtsEnable = false;
                sp.Open();

                Thread.Sleep(500); // Give Linux a moment to settle
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();

                sp.Write("PING\n");

                var timeout = DateTime.Now.AddSeconds(2.0);
                var buffer = "";

                while (DateTime.Now < timeout)
                {
                    if (sp.IsOpen && sp.BytesToRead > 0)
                    {
                        buffer += sp.ReadExisting();
                        if (buffer.Contains("START_APLIKACJA"))
                        {
                            Debug.WriteLine($"[C#] Handshake successful on {portName}");
                            return sp; // Return the OPEN port so we can reuse it
                        }
                    }

                    Thread.Sleep(50);
                }

                // If we get here, it wasn't the ESP32. Close it and try the next one.
                sp.Close();
                sp.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[C#] Error probing {portName}: {ex.Message}");
            }

        return null;
    }

    private void ChangeWifiButton_Click(object? sender, RoutedEventArgs e)
    {
        // 1. Re-enable the UI inputs
        InputPanel.IsEnabled = true;

        // 2. Clear the password box for security (optional, but good practice)
        PasswordTextBox.Text = "";

        // 3. Reset the status text
        StatusTextBlock.Text = "Status: Ready to configure new WiFi. Waking up ESP32...";
        StatusTextBlock.Foreground = Brushes.LightBlue; // Or whatever default color you like

        // 4. Automatically trigger a new scan to re-open the port and restart the ESP32
        // We can literally just call your existing Scan button logic!
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

        if (string.IsNullOrWhiteSpace(ssid))
        {
            StatusTextBlock.Text = "Status: SSID cannot be empty.";
            StatusTextBlock.Foreground = Brushes.Red;
            return;
        }

        StatusTextBlock.Text = "Status: Sending credentials...";
        StatusTextBlock.Foreground = Brushes.Orange;

        try
        {
            var isConfirmed = await Task.Run(() =>
            {
                _espPort.DiscardInBuffer();
                _espPort.DiscardOutBuffer();

                var overallTimeout = DateTime.Now.AddSeconds(6.0);
                var buffer = "";
                var commandSentOnce = false;

                while (DateTime.Now < overallTimeout)
                {
                    try
                    {
                        Debug.WriteLine(
                            $"[C#] Writing to {_espPort.PortName}: WIFI_CONFIG:{ssid}:{password}");
                        // Removed the \r so the ESP32 doesn't save it as part of the password!
                        _espPort.Write($"WIFI_CONFIG:{ssid}:{password}\n");
                        commandSentOnce = true;
                    }
                    catch (Exception ex)
                    {
                        // If writing fails AFTER we've successfully sent it once, 
                        // it means the ESP32 disconnected from USB because it restarted!
                        if (commandSentOnce)
                        {
                            Debug.WriteLine(
                                $"[C#] Port vanished on write. Assuming ESP32 restarted successfully! ({ex.Message})");
                            return true;
                        }

                        return false;
                    }

                    var chunkTimeout = DateTime.Now.AddSeconds(1.5);
                    while (DateTime.Now < chunkTimeout)
                    {
                        try
                        {
                            if (_espPort.IsOpen && _espPort.BytesToRead > 0)
                            {
                                var data = _espPort.ReadExisting();
                                buffer += data;
                                Debug.Write(data);

                                if (buffer.Contains("WIFI_CONFIRMED")) return true; // Clean confirmation
                            }
                        }
                        catch (Exception ex)
                        {
                            // If reading fails, same deal. The device rebooted.
                            Debug.WriteLine(
                                $"[C#] Port vanished on read. Assuming ESP32 restarted successfully! ({ex.Message})");
                            return true;
                        }

                        Thread.Sleep(50);
                    }

                    Debug.WriteLine("[C#] No response yet, retrying transmission...");
                }

                return false;
            });

            if (isConfirmed)
            {
                Debug.WriteLine("[C#] Credentials confirmed (or device rebooted)!");

                // 1. SAFELY UPDATE UI ON THE MAIN THREAD
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Debug.WriteLine("[C#] Drawing green success text...");
                    StatusTextBlock.Text = "Status: Success! ESP received credentials and is restarting.";
                    StatusTextBlock.Foreground = Brushes.Green;
                    InputPanel.IsEnabled = false;
                });

                // 2. PREVENT DEADLOCK: Throw the port cleanup into a background thread!
                Task.Run(() =>
                {
                    try
                    {
                        Debug.WriteLine("[C#] Attempting to close dead port in background...");
                        if (_espPort != null && _espPort.IsOpen) _espPort.Close();
                        _espPort?.Dispose();
                    }
                    catch
                    {
                        /* Ignore cleanup errors if device is gone */
                    }
                });
            }
            else
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusTextBlock.Text = "Status: Timeout. ESP did not confirm.";
                    StatusTextBlock.Foreground = Brushes.Red;
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"\n[C#] Connect Error: {ex.Message}");

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                StatusTextBlock.Foreground = Brushes.Red;
            });
        }
    }
}