using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using ScottPlot;
using ScottPlot.Plottables;

namespace UI_Test_Avalonia;

public partial class DataView : UserControl, IDisposable
{
    private readonly DispatcherTimer _renderTimer;
    private bool _isConnected;
    
    private readonly DataLogger? _logger1;
    private readonly DataLogger? _logger2;
    private readonly DataLogger? _loggerSum;

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
            
            // Create and style the three loggers
            _logger1 = WeightPlot.Plot.Add.DataLogger();
            _logger1.LegendText = "Left Scale";
            _logger1.Color = ScottPlot.Colors.CornflowerBlue;

            _logger2 = WeightPlot.Plot.Add.DataLogger();
            _logger2.LegendText = "Right Scale";
            _logger2.Color = ScottPlot.Colors.OrangeRed;

            _loggerSum = WeightPlot.Plot.Add.DataLogger();
            _loggerSum.LegendText = "Average";
            _loggerSum.Color = ScottPlot.Colors.Gray;
            _loggerSum.LineStyle.Pattern = LinePattern.Dotted;

            // Configure the X-axis to display time
            WeightPlot.Plot.Axes.DateTimeTicksBottom();            
            WeightPlot.Plot.YLabel("Weight (kg)");
            WeightPlot.Plot.Title("Live Asymmetry Data");
            WeightPlot.Plot.ShowLegend(Alignment.UpperLeft);
            
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

        // Check all three loggers are not null
        if (_logger1 != null && _logger2 != null && _loggerSum != null)
        {
            try
            {
                // Dequeue and plot data for each device using the timestamp
                while (MqttService.Instance.Device1Queue.TryDequeue(out var data))
                {
                    _logger1.Add(data.Timestamp.ToOADate(), data.Weight);
                    newDataRendered = true;
                }
                while (MqttService.Instance.Device2Queue.TryDequeue(out var data))
                {
                    _logger2.Add(data.Timestamp.ToOADate(), data.Weight);
                    newDataRendered = true;
                }
                while (MqttService.Instance.SumQueue.TryDequeue(out var data))
                {
                    _loggerSum.Add(data.Timestamp.ToOADate(), data.Weight);
                    newDataRendered = true;
                }
                
                if (newDataRendered)
                {
                    WeightPlot.Plot.Axes.AutoScale();
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