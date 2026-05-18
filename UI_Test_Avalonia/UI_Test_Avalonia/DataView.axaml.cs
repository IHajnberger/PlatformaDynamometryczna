using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ScottPlot.Plottables;

namespace UI_Test_Avalonia;

public partial class DataView : UserControl, IDisposable
{
    private readonly DispatcherTimer _renderTimer;
    private bool _isConnected;
    private readonly DataStreamer? _streamer1;

    public DataView()
    {
        InitializeComponent();
        Debug.WriteLine("[DataView] Constructor called.");

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20)
        };
        _renderTimer.Tick += RenderTimer_Tick;

        AttachedToVisualTree += (_, _) => 
        {
            Debug.WriteLine("[DataView] Attached to Window - Starting Render Timer.");
            _renderTimer.Start();
            WeightPlot.Refresh();
        };
        DetachedFromVisualTree += (_, _) => 
        {
            Debug.WriteLine("[DataView] Detached from Window - Stopping Render Timer.");
            _renderTimer.Stop();
        };

        try
        {
            Debug.WriteLine("[DataView] Initializing ScottPlot...");
            StatusText.Text = "Status: Waiting for MQTT Service...";
            StatusText.Foreground = Brushes.Orange;

            WeightPlot.Plot.Clear();
            _streamer1 = WeightPlot.Plot.Add.DataStreamer(500);
            _streamer1.ManageAxisLimits = true;

            WeightPlot.Plot.XLabel("Data Points");
            WeightPlot.Plot.YLabel("Weight (kg)");
            WeightPlot.Plot.Title("Live Weight Data");
            
            WeightPlot.Refresh();
            
            Debug.WriteLine("[DataView] Initialization complete.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FATAL] Unexpected Exception during init: {ex.Message}");
            ShowError($"An unexpected error occurred during startup:\n\n{ex.Message}");
        }
    }

    public void Dispose()
    {
        Debug.WriteLine("[DataView] Disposing...");
        _renderTimer.Stop();
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

    private void RenderTimer_Tick(object? sender, EventArgs e)
    {
        var wasConnected = _isConnected;
        
        // Access the singleton MqttService to check connection status
        bool isCurrentlyConnected = (DateTime.Now - MqttService.Instance.LastPacketTime).TotalMilliseconds < 4000;
        
        bool newDataRendered = false;

        if (isCurrentlyConnected != wasConnected)
        {
            _isConnected = isCurrentlyConnected;
            if (_isConnected)
            {
                StatusText.Text = "Status: Receiving data from ESP32 via MQTT...";
                StatusText.Foreground = Brushes.Green;
            }
            else
            {
                StatusText.Text = "Status: Connection lost. Waiting for ESP32 MQTT publish...";
                StatusText.Foreground = Brushes.Orange;
            }
        }

        if (_streamer1 != null)
        {
            try
            {
                // Dequeue all data from the central service
                while (MqttService.Instance.DataQueue.TryDequeue(out var weight))
                {
                    _streamer1.Add(weight);
                    newDataRendered = true;
                }
                
                if (newDataRendered)
                {
                    WeightPlot.Refresh();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DataView] Error during ScottPlot rendering: {ex.Message}");
            }
        }
    }
}