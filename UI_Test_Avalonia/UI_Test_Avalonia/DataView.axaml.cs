using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
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

namespace UI_Test_Avalonia;

public partial class DataView : UserControl, IDisposable
{
    public event EventHandler? BackClicked;
    private readonly DispatcherTimer _renderTimer;
    private bool _isConnected;
    
    // Główne kolekcje spięte z LiveCharts
    private readonly ObservableCollection<double> _leftValues = new();
    private readonly ObservableCollection<double> _rightValues = new();

    // Głębokie bufory dla kalkulatora parametrów biomechanicznych
    private readonly List<(double Weight, DateTime Timestamp)> _leftBuffer = new();
    private readonly List<(double Weight, DateTime Timestamp)> _rightBuffer = new();
    private const int BufferSize = 200; 
    private int _updateCounter = 0;

    public ObservableCollection<ISeries> LeftChartSeries { get; set; }
    public ObservableCollection<ISeries> RightChartSeries { get; set; }
    
    public Axis[] XAxesLeft { get; set; }
    public Axis[] XAxesRight { get; set; }
    public Axis[] YAxes { get; set; }
    public SolidColorPaint LegendPaint { get; set; } = new(SKColors.White);

    private readonly Axis _leftXAxis;
    private readonly Axis _rightXAxis;
    private const int ViewWindowSize = 100; // Rozmiar widocznego okna punktów

    // Flagi sterujące trybami automatycznego przewijania
    private bool _leftIsHistoryMode = false;
    private bool _rightIsHistoryMode = false;
    private bool _isUpdatingFromScroll = false;

    public DataView()
    {
        InitializeComponent();
        Debug.WriteLine("[DataView] Constructor called.");

        BackButton.Click += (sender, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        var leftColor = SKColor.Parse("#3b82f6");
        var rightColor = SKColor.Parse("#f59e0b");

        // Przywrócenie ustawień animacji dających stuprocentową płynność
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
                AnimationsSpeed = TimeSpan.FromMilliseconds(200),
                EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
            }
        };

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
                AnimationsSpeed = TimeSpan.FromMilliseconds(200),
                EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
            }
        };

        _leftXAxis = new Axis {
            TextSize = 0,
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1, PathEffect = new DashEffect(new float[] { 4, 4 }) },
            MinLimit = 0, MaxLimit = ViewWindowSize
        };

        _rightXAxis = new Axis {
            TextSize = 0,
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1, PathEffect = new DashEffect(new float[] { 4, 4 }) },
            MinLimit = 0, MaxLimit = ViewWindowSize
        };

        XAxesLeft = new Axis[] { _leftXAxis };
        XAxesRight = new Axis[] { _rightXAxis };

        YAxes = new Axis[] {
            new Axis {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
                TextSize = 11,
                Padding = new LiveChartsCore.Drawing.Padding(0, 0, 10, 0),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1 }
            }
        };

        DataContext = this;

        // Spięcie suwaków ze zdarzeniami pionowymi
        LeftScrollBar.Scroll += LeftScrollBar_Scroll;
        RightScrollBar.Scroll += RightScrollBar_Scroll;

        // Dynamiczne wykrywanie puszczenia myszy/dotyku nad wykresami, aby natychmiast sprawdzić czy wrócić do Live
        LeftChartContainer.PointerReleased += (s, e) => CheckIfReturnToLive(_leftXAxis, ref _leftIsHistoryMode, _leftValues.Count);
        RightChartContainer.PointerReleased += (s, e) => CheckIfReturnToLive(_rightXAxis, ref _rightIsHistoryMode, _rightValues.Count);

        // HUD - Przezroczystość pasków przewijania pojawiająca się na żądanie
        LeftChartContainer.PointerEntered += (s, e) => LeftScrollBar.Opacity = 0.8;
        LeftChartContainer.PointerExited += (s, e) => LeftScrollBar.Opacity = 0;
        RightChartContainer.PointerEntered += (s, e) => RightScrollBar.Opacity = 0.8;
        RightChartContainer.PointerExited += (s, e) => RightScrollBar.Opacity = 0;

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
            StatusText.Text = _isConnected ? "Status: Pobieranie danych z platform..." : "Status: Połączenie przerwane. Oczekiwanie na ESP32...";
            StatusText.Foreground = _isConnected ? Brushes.Green : Brushes.Orange;
        }

        // Konsumujemy pakiety bezpośrednio z MQTT do serii LiveCharts
        while (MqttService.Instance.Device1Queue.TryDequeue(out var data))
        {
            _leftValues.Add(data.Weight);
            _leftBuffer.Add(data);
        }
        while (MqttService.Instance.Device2Queue.TryDequeue(out var data))
        {
            _rightValues.Add(data.Weight);
            _rightBuffer.Add(data);
        }

        // Czyszczenie starej historii sesji z zachowaniem bezpiecznego bufora w RAM
        int maxCapacity = 4000;
        while (_leftValues.Count > maxCapacity) _leftValues.RemoveAt(0);
        while (_rightValues.Count > maxCapacity) _rightValues.RemoveAt(0);

        while (_leftBuffer.Count > BufferSize) _leftBuffer.RemoveAt(0);
        while (_rightBuffer.Count > BufferSize) _rightBuffer.RemoveAt(0);

        // --- OBSŁUGA AUTOMATYCZNEGO PRZESUWANIA OSI W TRYBIE LIVE ---
        UpdateAxisAndScrollBar(_leftValues.Count, _leftXAxis, LeftScrollBar, ref _leftIsHistoryMode);
        UpdateAxisAndScrollBar(_rightValues.Count, _rightXAxis, RightScrollBar, ref _rightIsHistoryMode);

        // Obliczenia statystyk biomechanicznych
        _updateCounter++;
        if (_updateCounter >= 10 && (_leftBuffer.Count > 0 || _rightBuffer.Count > 0))
        {
            _updateCounter = 0;
            var result = BiomechanicsService.Calculate(_leftBuffer, _rightBuffer);
            UpdateStats(result);
        }
    }

    private void UpdateAxisAndScrollBar(int totalPoints, Axis axis, ScrollBar scrollBar, ref bool isHistoryMode)
    {
        if (totalPoints <= ViewWindowSize) return;

        double maxScrollValue = totalPoints - ViewWindowSize;
        scrollBar.Maximum = maxScrollValue;
        scrollBar.ViewportSize = ViewWindowSize;

        // Jeśli oś została przesunięta myszką w lewo o więcej niż 5 punktów od krawędzi, aktywujemy tryb historii
        if (axis.MaxLimit < (totalPoints - 5))
        {
            isHistoryMode = true;
        }

        // Jeśli jesteśmy w trybie Live (nie przeglądamy historii), stabilnie i płynnie aktualizujemy położenie
        if (!isHistoryMode)
        {
            _isUpdatingFromScroll = true;
            scrollBar.Value = maxScrollValue;
            axis.MinLimit = maxScrollValue;
            axis.MaxLimit = totalPoints;
            _isUpdatingFromScroll = false;
        }
        else if (!_isUpdatingFromScroll)
        {
            // Jeśli użytkownik przesuwa myszką po wykresie, aktualizujemy pozycję kciuka dolnego suwaka w czasie rzeczywistym
            scrollBar.Value = axis.MinLimit ?? 0;
        }
    }

    // AUTOMATYCZNY POWRÓT DO LIVE PO DOSZEDŁU DO PRAWEJ GRANICY
    private void CheckIfReturnToLive(Axis axis, ref bool isHistoryMode, int totalPoints)
    {
        if (totalPoints <= ViewWindowSize) return;

        // Powrót: Jeśli maksymalny widok osi zostanie dociągnięty do krawędzi danych, włączamy autoscroll live
        if (axis.MaxLimit >= (totalPoints - 5))
        {
            isHistoryMode = false;
            StatusText.Text = "Status: Pobieranie danych z platform...";
            StatusText.Foreground = Brushes.Green;
        }
    }

    // OBSŁUGA RĘCZNEGO PRZESUWANIA SUWAKIEM POD SPODEM
    private void LeftScrollBar_Scroll(object? sender, ScrollEventArgs e)
    {
        if (_isUpdatingFromScroll) return;

        _isUpdatingFromScroll = true;
        _leftXAxis.MinLimit = e.NewValue;
        _leftXAxis.MaxLimit = e.NewValue + ViewWindowSize;
        _isUpdatingFromScroll = false;

        CheckIfReturnToLive(_leftXAxis, ref _leftIsHistoryMode, _leftValues.Count);
    }

    private void RightScrollBar_Scroll(object? sender, ScrollEventArgs e)
    {
        if (_isUpdatingFromScroll) return;

        _isUpdatingFromScroll = true;
        _rightXAxis.MinLimit = e.NewValue;
        _rightXAxis.MaxLimit = e.NewValue + ViewWindowSize;
        _isUpdatingFromScroll = false;

        CheckIfReturnToLive(_rightXAxis, ref _rightIsHistoryMode, _rightValues.Count);
    }

    private void UpdateStats(BiomechanicsResult r)
    {
        Dispatcher.UIThread.Post(() =>
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
        }, DispatcherPriority.Render);
    }
}