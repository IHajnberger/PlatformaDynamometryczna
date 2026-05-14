using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ScottPlot.Plottables;

namespace UI_Test_Avalonia;

public partial class DataView : UserControl, IDisposable
{
    private const int UdpPort = 12345;

    // --- Thread-safe buffering and rendering ---z
    private readonly DispatcherTimer _renderTimer;
    private bool _isConnected;
    private volatile bool _isListening = true;

    // Use a ConcurrentQueue to prevent data loss if packets arrive faster than 20ms
    private readonly ConcurrentQueue<(double Weight1, double Weight2)> _dataQueue = new();
    private DateTime _lastPacketTime = DateTime.MinValue;
    private readonly Thread? _listenThread;

    private readonly DataStreamer? _streamer1;
    private readonly DataStreamer? _streamer2;
    private readonly UdpClient? _udpClient;

    private int _debugPacketCount = 0; 

    public DataView()
    {
        InitializeComponent();
        Debug.WriteLine("[UI] DataView Constructor called.");

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20)
        };
        _renderTimer.Tick += RenderTimer_Tick;

        AttachedToVisualTree += (_, _) => 
        {
            Debug.WriteLine("[UI] DataView Attached to Window - Starting Render Timer.");
            _renderTimer.Start();
            WeightPlot.Refresh(); // Force initial render when attached
        };
        DetachedFromVisualTree += (_, _) => 
        {
            Debug.WriteLine("[UI] DataView Detached from Window - Stopping Render Timer.");
            _renderTimer.Stop();
        };

        try
        {
            Debug.WriteLine("[UI] Initializing ScottPlot...");
            StatusText.Text = "Status: Initializing...";
            StatusText.Foreground = Brushes.Orange;

            _streamer1 = WeightPlot.Plot.Add.DataStreamer(500);
            _streamer2 = WeightPlot.Plot.Add.DataStreamer(500);

            _streamer1.ManageAxisLimits = true;
            _streamer2.ManageAxisLimits = true;

            WeightPlot.Plot.XLabel("Data Points");
            WeightPlot.Plot.YLabel("Weight (kg)");
            WeightPlot.Plot.Title("Live Weight Data");

            Debug.WriteLine($"[Network] Attempting to bind UDP listener on Port {UdpPort}...");
            _udpClient = new UdpClient(UdpPort);
            
            _listenThread = new Thread(ListenForPackets) { IsBackground = true };
            _listenThread.Start();

            StatusText.Text = "Status: Listening for UDP packets. Please connect the device...";
            // We removed _renderTimer.Start() here so it only ticks when visible.
            
            // Render at least once so it's not blank
            WeightPlot.Refresh();
            
            Debug.WriteLine("[UI] Initialization complete. Waiting for data.");
        }
        catch (SocketException ex)
        {
            Debug.WriteLine($"[FATAL] Socket Exception during init: {ex.Message}");
            ShowError($"Failed to start UDP listener. Is another instance of the app running?\n\nDetails: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FATAL] Unexpected Exception during init: {ex.Message}");
            ShowError($"An unexpected error occurred during startup:\n\n{ex.Message}");
        }
    }

    public void Dispose()
    {
        Debug.WriteLine("[System] Disposing DataView...");
        if (!_isListening) return;

        _isListening = false;
        _renderTimer.Stop();

        Debug.WriteLine("[System] Closing UDP Client...");
        _udpClient?.Close();
        _udpClient?.Dispose();
        Debug.WriteLine("[System] DataView Disposed Cleanly.");
    }

    private void ShowError(string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            MainPanel.IsVisible = false;
            ErrorText.IsVisible = true;
            ErrorText.Text = message;
        });
    }

    private void ListenForPackets()
    {
        Debug.WriteLine("[Network] Background UDP Thread started.");
        if (_udpClient == null) return;
        var remoteEp = new IPEndPoint(IPAddress.Any, UdpPort);

        try
        {
            while (_isListening)
            {
                var data = _udpClient.Receive(ref remoteEp);
                var message = Encoding.ASCII.GetString(data);

                // Add aggressive debugging to figure out why parsing might be failing
                _debugPacketCount++;

                var parts = message.Split(',');
                
                if (parts.Length != 3)
                {
                    // If it doesn't have 3 parts, it might be a standard log message from the ESP rather than sensor data.
                    Debug.WriteLine($"[ESP LOG] {message} (From: {remoteEp})");
                    continue; // Skip to next packet
                }

                // Print every packet to debug since the ESP is currently sending at 1 Hz
                Debug.WriteLine($"[Network] RAW DATA UDP Packet: '{message}'");

                bool parsedW1 = double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var weight1);
                bool parsedW2 = double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var weight2);

                if (!parsedW1 || !parsedW2)
                {
                    Debug.WriteLine($"[Parse Error] Failed to parse doubles! W1 string: '{parts[1]}', W2 string: '{parts[2]}'");
                    continue; // Skip to next packet
                }

                // If we get here, the packet was perfect.
                _dataQueue.Enqueue((weight1, weight2));
                _lastPacketTime = DateTime.Now;
            }
        }
        catch (SocketException)
        {
            Debug.WriteLine("[Network] SocketException caught (Expected if app is closing).");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Network] FATAL background thread error: {ex.Message}\n{ex.StackTrace}");
            if (_isListening) ShowError($"UDP listener error: {ex.Message}");
        }
        Debug.WriteLine("[Network] Background UDP Thread exited.");
    }

    private void RenderTimer_Tick(object? sender, EventArgs e)
    {
        var wasConnected = _isConnected;
        var isCurrentlyConnected = false;
        
        // Calculate if we've received data within the last second
        isCurrentlyConnected = (DateTime.Now - _lastPacketTime).TotalMilliseconds < 1000;
        
        bool newDataRendered = false;

        if (isCurrentlyConnected != wasConnected)
        {
            _isConnected = isCurrentlyConnected;
            if (_isConnected)
            {
                Debug.WriteLine("[UI] Connection State Changed: CONNECTED / RECEIVING");
                StatusText.Text = "Status: Receiving data...";
                StatusText.Foreground = Brushes.Green;
            }
            else
            {
                Debug.WriteLine("[UI] Connection State Changed: DISCONNECTED / TIMEOUT");
                StatusText.Text = "Status: Connection lost. Please connect the device...";
                StatusText.Foreground = Brushes.Orange;
            }
        }

        // Empty the queue completely every tick, plotting every single data point received
        if (_streamer1 != null && _streamer2 != null)
        {
            try
            {
                while (_dataQueue.TryDequeue(out var data))
                {
                    _streamer1.Add(data.Weight1);
                    _streamer2.Add(data.Weight2);
                    newDataRendered = true;
                }
                
                if (newDataRendered)
                {
                    WeightPlot.Refresh();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UI] Error during ScottPlot rendering: {ex.Message}");
            }
        }
    }
}