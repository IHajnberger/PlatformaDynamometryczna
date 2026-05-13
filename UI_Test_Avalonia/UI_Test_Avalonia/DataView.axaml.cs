using System;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
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

    // --- Thread-safe buffering and rendering ---
    private readonly object _bufferLock = new();
    private readonly DispatcherTimer _renderTimer;
    private bool _isConnected;
    private volatile bool _isListening = true;

    private DateTime _lastPacketTime = DateTime.MinValue;
    private double _lastWeight1;
    private double _lastWeight2;
    private readonly Thread? _listenThread;

    private bool _newDataAvailable;

    private readonly DataStreamer? _streamer1;
    private readonly DataStreamer? _streamer2;
    private readonly UdpClient? _udpClient;

    // Added a counter to prevent console spam while still proving it works
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

                // DEBUG: Print the raw message every 50 packets to avoid lagging the console,
                // OR print it immediately if it's the very first packet.
                _debugPacketCount++;
                if (_debugPacketCount == 1 || _debugPacketCount % 50 == 0)
                {
                    Debug.WriteLine($"[Network] RAW UDP Packet received: '{message}' (From: {remoteEp})");
                }

                var parts = message.Split(',');
                
                // Add aggressive debugging to figure out why parsing might be failing
                if (parts.Length != 3)
                {
                    Debug.WriteLine($"[Parse Error] Packet rejected! Expected 3 parts, got {parts.Length}. Raw string: '{message}'");
                    continue; // Skip to next packet
                }

                bool parsedW1 = double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var weight1);
                bool parsedW2 = double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var weight2);

                if (!parsedW1 || !parsedW2)
                {
                    Debug.WriteLine($"[Parse Error] Failed to parse doubles! W1 string: '{parts[1]}', W2 string: '{parts[2]}'");
                    continue; // Skip to next packet
                }

                // If we get here, the packet was perfect.
                lock (_bufferLock)
                {
                    _lastWeight1 = weight1;
                    _lastWeight2 = weight2;
                    _newDataAvailable = true;
                    _lastPacketTime = DateTime.Now;
                }
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
        double currentWeight1 = 0;
        double currentWeight2 = 0;
        var shouldRender = false;

        var wasConnected = _isConnected;
        var isCurrentlyConnected = false;

        lock (_bufferLock)
        {
            if (_newDataAvailable)
            {
                currentWeight1 = _lastWeight1;
                currentWeight2 = _lastWeight2;
                _newDataAvailable = false;
                shouldRender = true;
            }

            isCurrentlyConnected = (DateTime.Now - _lastPacketTime).TotalMilliseconds < 1000;
        }

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

        if (shouldRender && _streamer1 != null && _streamer2 != null)
        {
            try
            {
                _streamer1.Add(currentWeight1);
                _streamer2.Add(currentWeight2);
                WeightPlot.Refresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UI] Error during ScottPlot rendering: {ex.Message}");
            }
        }
    }
} 