using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using UI_Test_Avalonia.Services;

namespace UI_Test_Avalonia;

public partial class DataView : UserControl, IDisposable
{
    public event EventHandler? BackClicked;
    private readonly DispatcherTimer _renderTimer;
    private bool _isConnected;
    
    private readonly ObservableCollection<double> _leftValues = new();
    private readonly ObservableCollection<double> _rightValues = new();

    // Pod Bioservice
    private readonly List<(double Weight, DateTime Timestamp)> _leftBuffer = new();
    private readonly List<(double Weight, DateTime Timestamp)> _rightBuffer = new();
    private const int BufferSize = 200; // ostatnie 200 próbek do obliczeń
    private int _updateCounter = 0;

    // Dwie niezależne kolekcje serii dla osobnych wykresów
    public ObservableCollection<ISeries> LeftChartSeries { get; set; }
    public ObservableCollection<ISeries> RightChartSeries { get; set; }
    
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    public SolidColorPaint LegendPaint { get; set; } = new(SKColors.White);

    public DataView()
    {
        InitializeComponent();
        Debug.WriteLine("[DataView] Constructor called.");

        BackButton.Click += (sender, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        var leftColor = SKColor.Parse("#3b82f6");
        var rightColor = SKColor.Parse("#f59e0b");

        // SERIA DLA LEWEGO WYKRESU (Niebieska)
        LeftChartSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<double>
            {
                Name = "Left Scale",
                Values = _leftValues,
                GeometrySize = 0,
                LineSmoothness = 0.75,
                Stroke = new SolidColorPaint(leftColor) { StrokeThickness = 4 },
                Fill = new LinearGradientPaint(
                    new[] { leftColor.WithAlpha(40), leftColor.WithAlpha(0) },
                    new SKPoint(0.5f, 0),
                    new SKPoint(0.5f, 1)),
                AnimationsSpeed = TimeSpan.FromMilliseconds(350),
                EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
            }
        };

        // SERIA DLA PRAWEGO WYKRESU (Pomarańczowa)
        RightChartSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<double>
            {
                Name = "Right Scale",
                Values = _rightValues,
                GeometrySize = 0,
                LineSmoothness = 0.75,
                Stroke = new SolidColorPaint(rightColor) { StrokeThickness = 4 },
                Fill = new LinearGradientPaint(
                    new[] { rightColor.WithAlpha(40), rightColor.WithAlpha(0) },
                    new SKPoint(0.5f, 0),
                    new SKPoint(0.5f, 1)),
                AnimationsSpeed = TimeSpan.FromMilliseconds(350),
                EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
            }
        };

        // OSI I SIATKA (Identyczne, minimalistyczne reguły jak wcześniej)
        XAxes = new Axis[] {
            new Axis {
                TextSize = 0,
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) 
                { 
                    StrokeThickness = 1,
                    PathEffect = new DashEffect(new float[] { 4, 4 })
                }
            }
        };

        YAxes = new Axis[] {
            new Axis {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
                TextSize = 11,
                Padding = new LiveChartsCore.Drawing.Padding(0, 0, 10, 0),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1 }
            }
        };

        DataContext = this;

        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _renderTimer.Tick += RenderTimer_Tick;

        AttachedToVisualTree += (_, _) => _renderTimer.Start();
        DetachedFromVisualTree += (_, _) => _renderTimer.Stop();
    }

    public void Dispose() => _renderTimer.Stop();
    private void RenderTimer_Tick(object? sender, EventArgs e)
    {
        bool isCurrentlyConnected = (DateTime.Now - ConfigureWifiView.LastPacketTime).TotalMilliseconds < 4000;

        if (isCurrentlyConnected != _isConnected)
        {
            _isConnected = isCurrentlyConnected;
            StatusText.Text = _isConnected
                ? "Status: Pobieranie danych z platform..."
                : "Status: Połączenie przerwane. Oczekiwanie na ESP32...";
            StatusText.Foreground = _isConnected ? Brushes.Green : Brushes.Orange;
        }

        while (ConfigureWifiView.Device1Queue.TryDequeue(out var data))
        {
            _leftValues.Add(data.Weight);
            _leftBuffer.Add(data);
        }
        while (ConfigureWifiView.Device2Queue.TryDequeue(out var data))
        {
            _rightValues.Add(data.Weight);
            _rightBuffer.Add(data);
        }

        int maxPoints = 70;
        while (_leftValues.Count > maxPoints) _leftValues.RemoveAt(0);
        while (_rightValues.Count > maxPoints) _rightValues.RemoveAt(0);

        while (_leftBuffer.Count > BufferSize) _leftBuffer.RemoveAt(0);
        while (_rightBuffer.Count > BufferSize) _rightBuffer.RemoveAt(0);

        // Licz parametry co 10 ticków (~300ms) żeby nie obciążać UI
        _updateCounter++;
        if (_updateCounter >= 10 && _leftBuffer.Count > 10)
        {
            _updateCounter = 0;
            var result = BiomechanicsService.Calculate(_leftBuffer, _rightBuffer);
            UpdateStats(result);
        }
    }

    private void UpdateStats(BiomechanicsResult r)
    {
        PeakForceLText.Text = $"{r.PeakForceLeft:F1} kg";
        PeakForceRText.Text = $"{r.PeakForceRight:F1} kg";
        MeanForceLText.Text = $"{r.MeanForceLeft:F1} kg";
        MeanForceRText.Text = $"{r.MeanForceRight:F1} kg";
        LoadRatioText.Text = $"L:{r.LoadRatioLeft:F1}% R:{r.LoadRatioRight:F1}%";
        AsymmetryText.Text = $"{r.AsymmetryIndex:F1}%";
        FlightTimeText.Text = r.FlightTime > 0 ? $"{r.FlightTime:F3} s" : "--";
        BrakingRFDText.Text = $"{r.BrakingRFD:F1} kg/s";

        SummaryPeakText.Text = $"{r.PeakForceTotal:F1} kg";
        SummaryMeanText.Text = $"{r.MeanForceTotal:F1} kg";
        SummaryMinText.Text = $"{Math.Min(r.MinForceLeft, r.MinForceRight):F1} kg";
        SummaryAsymmetryText.Text = $"{r.AsymmetryIndex:F1}%";

        BalanceLeftText.Text = $"L: {r.LoadRatioLeft:F1}%";
        BalanceRightText.Text = $"R: {r.LoadRatioRight:F1}%";
    }

}