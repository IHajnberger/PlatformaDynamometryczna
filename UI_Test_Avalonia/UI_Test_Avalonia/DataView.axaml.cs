using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using LiveChartsCore;
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
    
    private readonly ObservableCollection<double> _leftValues = new();
    private readonly ObservableCollection<double> _rightValues = new();

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
        bool isCurrentlyConnected = (DateTime.Now - MqttService.Instance.LastPacketTime).TotalMilliseconds < 4000;

        if (isCurrentlyConnected != _isConnected)
        {
            _isConnected = isCurrentlyConnected;
            StatusText.Text = _isConnected ? "Status: Pobieranie danych z platform..." : "Status: Połączenie przerwane. Oczekiwanie na ESP32...";
            StatusText.Foreground = _isConnected ? Brushes.Green : Brushes.Orange;
        }

        // Dequeue paczek tylko dla urządzeń 1 i 2 (suma usunięta)
        while (MqttService.Instance.Device1Queue.TryDequeue(out var data))
        {
            _leftValues.Add(data.Weight);
        }
        while (MqttService.Instance.Device2Queue.TryDequeue(out var data))
        {
            _rightValues.Add(data.Weight);
        }

        // Czyszczenie starego bufora (utrzymujemy fluid effect do 70 próbek)
        int maxPoints = 70;
        while (_leftValues.Count > maxPoints) _leftValues.RemoveAt(0);
        while (_rightValues.Count > maxPoints) _rightValues.RemoveAt(0);
    }
}