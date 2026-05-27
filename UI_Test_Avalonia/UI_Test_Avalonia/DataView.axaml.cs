using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;

namespace UI_Test_Avalonia;

public partial class DataView : UserControl, IDisposable
{
    public event EventHandler? BackClicked;
    private readonly DispatcherTimer _renderTimer;
    private bool _isConnected;
    
    private readonly ObservableCollection<DateTimePoint> _leftValues = new();
    private readonly ObservableCollection<DateTimePoint> _rightValues = new();

    private const int FilterSize = 1; 
    private readonly List<double> _leftBuffer = new();
    private readonly List<double> _rightBuffer = new();

    private double _maxForce = 1.0;

    public ObservableCollection<ISeries> LeftChartSeries { get; set; }
    public ObservableCollection<ISeries> RightChartSeries { get; set; }
    
    public Axis[] XAxesLeft { get; set; }
    public Axis[] XAxesRight { get; set; }
    public Axis[] YAxes { get; set; }
    public SolidColorPaint LegendPaint { get; set; } = new(SKColors.White);

    public DataView()
    {
        InitializeComponent();
        Debug.WriteLine("[DataView] Constructor called.");

        BackButton.Click += (sender, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        var leftColor = SKColor.Parse("#3b82f6");
        var rightColor = SKColor.Parse("#f59e0b");

        LeftChartSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Name = "Left Scale",
                Values = _leftValues,
                GeometrySize = 0,
                LineSmoothness = 0.6,
                Stroke = new SolidColorPaint(leftColor) { StrokeThickness = 4 },
                Fill = new LinearGradientPaint(new[] { leftColor.WithAlpha(40), leftColor.WithAlpha(0) }, new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                AnimationsSpeed = TimeSpan.FromMilliseconds(100),
                EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
            }
        };

        RightChartSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Name = "Right Scale",
                Values = _rightValues,
                GeometrySize = 0,
                LineSmoothness = 0.6,
                Stroke = new SolidColorPaint(rightColor) { StrokeThickness = 4 },
                Fill = new LinearGradientPaint(new[] { rightColor.WithAlpha(40), rightColor.WithAlpha(0) }, new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                AnimationsSpeed = TimeSpan.FromMilliseconds(100),
                EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
            }
        };

        var axisStyle = new Axis {
            UnitWidth = TimeSpan.FromMilliseconds(1).Ticks,
            TextSize = 12,
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) 
            { 
                StrokeThickness = 1,
                PathEffect = new DashEffect(new float[] { 4, 4 })
            }
        };
        XAxesLeft = new[] { axisStyle };
        XAxesRight = new[] { axisStyle };

        YAxes = new[] {
            new Axis {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
                TextSize = 11,
                Padding = new LiveChartsCore.Drawing.Padding(0, 0, 10, 0),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1 },
                MinLimit = 0,
                MaxLimit = _maxForce 
            }
        };

        DataContext = this;

        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _renderTimer.Tick += RenderTimer_Tick;

        AttachedToVisualTree += (_, _) => _renderTimer.Start();
        DetachedFromVisualTree += (_, _) => _renderTimer.Stop();
    }

    public void Dispose() => _renderTimer.Stop();

    private void RenderTimer_Tick(object? sender, EventArgs e)
    {
        bool isCurrentlyConnected = (DateTime.Now - MqttService.Instance.LastPacketTime).TotalMilliseconds < 4000;

        if (isCurrentlyConnected != _isConnected)
        {
            _isConnected = isCurrentlyConnected;
            StatusText.Text = _isConnected ? "Status: Receiving data..." : "Status: Connection lost. Waiting for ESP32...";
            StatusText.Foreground = _isConnected ? Brushes.Green : Brushes.Orange;
        }

        while (MqttService.Instance.Device1Queue.TryDequeue(out var data))
        {
            _leftBuffer.Add(data.Weight);
            if (_leftBuffer.Count > FilterSize) _leftBuffer.RemoveAt(0);
            var smoothedValue = _leftBuffer.Average();
            _leftValues.Add(new DateTimePoint(data.Timestamp, smoothedValue));
            if (smoothedValue > _maxForce) _maxForce = smoothedValue;
        }
        
        while (MqttService.Instance.Device2Queue.TryDequeue(out var data))
        {
            _rightBuffer.Add(data.Weight);
            if (_rightBuffer.Count > FilterSize) _rightBuffer.RemoveAt(0);
            var smoothedValue = _rightBuffer.Average();
            _rightValues.Add(new DateTimePoint(data.Timestamp, smoothedValue));
            if (smoothedValue > _maxForce) _maxForce = smoothedValue;
        }

        if (YAxes[0].MaxLimit < _maxForce)
        {
            YAxes[0].MaxLimit = _maxForce * 1.1;
        }

        var now = DateTime.Now;
        var limit = now.AddSeconds(-5); 
        
        while (_leftValues.Count > 0 && _leftValues[0].DateTime < limit) _leftValues.RemoveAt(0);
        while (_rightValues.Count > 0 && _rightValues[0].DateTime < limit) _rightValues.RemoveAt(0);
    }
}