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
    
    // We now need three streamers for the three data lines
    private readonly DataStreamer? _streamer1;
    private readonly DataStreamer? _streamer2;
    private readonly DataStreamer? _streamerAvg;

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
            
            // Create and style the three streamers
            _streamer1 = WeightPlot.Plot.Add.DataStreamer(500);
            _streamer1.ManageAxisLimits = true;
            _streamer1.LegendText = "Left Scale";
            _streamer1.Color = ScottPlot.Colors.CornflowerBlue;

            _streamer2 = WeightPlot.Plot.Add.DataStreamer(500);
            _streamer2.ManageAxisLimits = true;
            _streamer2.LegendText = "Right Scale";
            _streamer2.Color = ScottPlot.Colors.OrangeRed;

            _streamerAvg = WeightPlot.Plot.Add.DataStreamer(500);
            _streamerAvg.ManageAxisLimits = true;
            _streamerAvg.LegendText = "Average";
            _streamerAvg.Color = ScottPlot.Colors.Gray;
            _streamerAvg.LinePattern = ScottPlot.LinePattern.Dotted;

            WeightPlot.Plot.XLabel("Data Points");
            WeightPlot.Plot.YLabel("Weight (kg)");
            WeightPlot.Plot.Title("Live Asymmetry Data");
            
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
        
        bool isCurrentlyConnected = (DateTime.Now - MqttService.Instance.LastPacketTime).TotalMilliseconds < 4000;
        
        bool newDataRendered = false;

        if (isCurrentlyConnected != wasConnected)
        {
            _isConnected = isCurrentlyConnected;
            if (_isConnected)
            {
                StatusText.Text = "Status: Receiving data from ESP32 devices...";
                StatusText.Foreground = Brushes.Green;
            }
            else
            {
                StatusText.Text = "Status: Connection lost. Waiting for ESP32 MQTT publish...";
                StatusText.Foreground = Brushes.Orange;
            }
        }

        // Check all three streamers are not null
        if (_streamer1 != null && _streamer2 != null && _streamerAvg != null)
        {
            try
            {
                // Dequeue and plot data for each device
                while (MqttService.Instance.Device1Queue.TryDequeue(out var weight1))
                {
                    _streamer1.Add(weight1);
                    newDataRendered = true;
                }
                while (MqttService.Instance.Device2Queue.TryDequeue(out var weight2))
                {
                    _streamer2.Add(weight2);
                    newDataRendered = true;
                }
                while (MqttService.Instance.AverageQueue.TryDequeue(out var avg))
                {
                    _streamerAvg.Add(avg);
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