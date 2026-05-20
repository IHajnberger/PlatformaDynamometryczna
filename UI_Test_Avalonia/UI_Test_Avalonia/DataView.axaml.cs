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
    private readonly ObservableCollection<double> _sumValues = new();

    public ObservableCollection<ISeries> ChartSeries { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    public SolidColorPaint LegendPaint { get; set; } = new(SKColors.White);

    public DataView()
    {
        InitializeComponent();
        Debug.WriteLine("[DataView] Constructor called.");

        BackButton.Click += (sender, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        // KROK 1: ULTRA-PŁYNNE GRADIENTY I EMISJA ŚWIATŁA (React / Tailwind Style)
        var leftColor = SKColor.Parse("#3b82f6");
        var rightColor = SKColor.Parse("#f59e0b");
        var sumColor = SKColor.Parse("#10b981");

        ChartSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<double>
            {
                Name = "Left Scale",
                Values = _leftValues,
                GeometrySize = 0,                 // Zero punktów (czysta, gładka wstęga)
                LineSmoothness = 0.75,            // Wyższy współczynnik wygładzania dla super opływowych krzywych
                Stroke = new SolidColorPaint(leftColor) { StrokeThickness = 4 }, // Grubsza, wyraźniejsza linia
                
                // Nowoczesny, zanikający gradient pod wykresem (Area Chart)
                Fill = new LinearGradientPaint(
                    new[] { leftColor.WithAlpha(40), leftColor.WithAlpha(0) },
                    new SKPoint(0.5f, 0),         // Początek gradientu (góra)
                    new SKPoint(0.5f, 1)),        // Koniec gradientu (dół)
                
                // Płynna, dynamiczna fizyka ruchu (Easing)
                AnimationsSpeed = TimeSpan.FromMilliseconds(350),
                EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
            },
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
            },
            new LineSeries<double>
            {
                Name = "Sum",
                Values = _sumValues,
                GeometrySize = 0,
                LineSmoothness = 0.75,
                // Linia sumy jako elegancka, przerywana linia (Dash Path Effect)
                Stroke = new SolidColorPaint(sumColor) 
                { 
                    StrokeThickness = 2,
                    PathEffect = new DashEffect(new float[] { 6, 6 }) // 6px kreski, 6px przerwy
                },
                Fill = null,
                AnimationsSpeed = TimeSpan.FromMilliseconds(350),
                EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
            }
        };

        // KROK 2: MINIMALISTYCZNE, NIEODWRACAJĄCE UWAGI OSI
        XAxes = new Axis[] {
            new Axis {
                TextSize = 0, // Ukryte podpisy osi X dla idealnej płynności strumienia danych
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) 
                { 
                    StrokeThickness = 1,
                    PathEffect = new DashEffect(new float[] { 4, 4 }) // Kropkowane linie siatki (bardzo modernistyczne)
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

        // KROK 3: ZWIĘKSZENIE CZĘSTOTLIWOŚCI PRÓBKOWANIA DLA "FLUID EFFECT"
        // Zmniejszamy interwał z 40ms do 30ms, aby wykres był bardziej dynamiczny
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

        // Dequeue danych z brokera MQTT
        while (MqttService.Instance.Device1Queue.TryDequeue(out var data))
        {
            _leftValues.Add(data.Weight);
        }
        while (MqttService.Instance.Device2Queue.TryDequeue(out var data))
        {
            _rightValues.Add(data.Weight);
        }
        while (MqttService.Instance.SumQueue.TryDequeue(out var data))
        {
            _sumValues.Add(data.Weight);
        }

        // Zwiększamy bufor wyświetlania z 50 do 70 punktów, 
        // dzięki czemu fala płynie wolniej i bardziej dostojnie (fluid effect)
        int maxPoints = 70;
        while (_leftValues.Count > maxPoints) _leftValues.RemoveAt(0);
        while (_rightValues.Count > maxPoints) _rightValues.RemoveAt(0);
        while (_sumValues.Count > maxPoints) _sumValues.RemoveAt(0);
    }
}