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
using UI_Test_Avalonia.Services;

namespace UI_Test_Avalonia;

public partial class DataView : UserControl, IDisposable
{
    private BiomechanicsResult? _lastCalculatedResult;
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
    private readonly Axis _sharedYAxis;
    private const int ViewWindowSize = 100; // Rozmiar widocznego okna punktów

    // --- KLUCZOWA ZMIENNA: Przechowuje maksymalną wartość Y całej sesji ---
    private double _sessionMaxY = 5.0; // Domyślne minimum, żeby pusty wykres nie był spłaszczony

    // Domyślnie aplikacja startuje w trybie Live (HistoryMode = false)
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

                GeometryFill = new SolidColorPaint(rightColor),
                GeometryStroke = new SolidColorPaint(rightColor) { StrokeThickness = 2 },

                AnimationsSpeed = TimeSpan.FromMilliseconds(200),
                EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
            }
        };

        _leftXAxis = new Axis
        {
            TextSize = 0,
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1, PathEffect = new DashEffect(new float[] { 4, 4 }) },
            MinLimit = 0,
            MaxLimit = ViewWindowSize
        };

        _rightXAxis = new Axis
        {
            TextSize = 0,
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1, PathEffect = new DashEffect(new float[] { 4, 4 }) },
            MinLimit = 0,
            MaxLimit = ViewWindowSize
        };

        XAxesLeft = new Axis[] { _leftXAxis };
        XAxesRight = new Axis[] { _rightXAxis };

        _sharedYAxis = new Axis
        {
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
            TextSize = 11,
            Padding = new LiveChartsCore.Drawing.Padding(0, 0, 10, 0),
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1 },
            MinLimit = 0,
            MaxLimit = _sessionMaxY // Na starcie przypisujemy domyślne 5.0
        };

        YAxes = new Axis[] { _sharedYAxis };

        DataContext = this;

        LeftScrollBar.Scroll += LeftScrollBar_Scroll;
        RightScrollBar.Scroll += RightScrollBar_Scroll;

        LeftChartContainer.PointerReleased += (s, e) => CheckIfReturnToLive(_leftXAxis, ref _leftIsHistoryMode, _leftValues.Count);
        RightChartContainer.PointerReleased += (s, e) => CheckIfReturnToLive(_rightXAxis, ref _rightIsHistoryMode, _rightValues.Count);

        LeftChartContainer.PointerEntered += (s, e) => LeftScrollBar.Opacity = 0.8;
        LeftChartContainer.PointerExited += (s, e) => LeftScrollBar.Opacity = 0;
        RightChartContainer.PointerEntered += (s, e) => RightScrollBar.Opacity = 0.8;
        RightChartContainer.PointerExited += (s, e) => RightScrollBar.Opacity = 0;

        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _renderTimer.Tick += RenderTimer_Tick;

        AttachedToVisualTree += (_, _) => _renderTimer.Start();
        DetachedFromVisualTree += (_, _) => _renderTimer.Stop();
        var saveButton = this.FindControl<Button>("SaveSessionButton") ?? this.Find<Button>("SaveSessionButton");
        if (saveButton != null)
        {
            saveButton.Click += (sender, e) =>
            {
                var activePatient = PatientService.Instance.ActivePatient;
                if (activePatient == null)
                {
                    StatusText.Text = "Status: Błąd! Brak aktywnego pacjenta. Nie można zapisać sesji.";
                    StatusText.Foreground = Brushes.Red;
                    return;
                }

                // Przygotowanie danych do serializacji wykresu (bierzemy pod uwagę maxCapacity punktów)
                var leftPoints = new List<double>(_leftValues);
                var rightPoints = new List<double>(_rightValues);

                var session = new PatientSession
                {
                    PatientId = activePatient.Id,
                    Date = DateTime.Now,
                    ExerciseName = "Próba statyczna / dynamiczna platform",
                    PeakForceLeft = _lastCalculatedResult?.PeakForceLeft ?? 0,
                    PeakForceRight = _lastCalculatedResult?.PeakForceRight ?? 0,
                    MeanForceLeft = _lastCalculatedResult?.MeanForceLeft ?? 0,
                    MeanForceRight = _lastCalculatedResult?.MeanForceRight ?? 0,
                    AsymmetryIndex = _lastCalculatedResult?.AsymmetryIndex ?? 0,
                    RawLeftValuesJson = System.Text.Json.JsonSerializer.Serialize(leftPoints),
                    RawRightValuesJson = System.Text.Json.JsonSerializer.Serialize(rightPoints)
                };

                SessionService.Instance.SaveSession(session);

                StatusText.Text = $"Status: Pomyślnie zapisano sesję dla {activePatient.FullName}!";
                StatusText.Foreground = Brushes.LightGreen;
            };
        }
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

        // Pobieranie pakietów z MQTT - LEWA PLATFORMA
        while (MqttService.Instance.Device1Queue.TryDequeue(out var data))
        {
            double filteredWeight = data.Weight < 0.5 ? 0.0 : data.Weight;
            data = data with { Weight = filteredWeight };

            _leftValues.Add(filteredWeight);
            _leftBuffer.Add(data);

            // Sprawdzamy szczyt "w locie" bez obciążającego LINQ
            if (filteredWeight > _sessionMaxY)
            {
                _sessionMaxY = filteredWeight;
            }
        }

        // Pobieranie pakietów z MQTT - PRAWA PLATFORMA
        while (MqttService.Instance.Device2Queue.TryDequeue(out var data))
        {
            double filteredWeight = data.Weight < 0.5 ? 0.0 : data.Weight;
            data = data with { Weight = filteredWeight };

            _rightValues.Add(filteredWeight);
            _rightBuffer.Add(data);

            // Sprawdzamy szczyt "w locie" bez obciążającego LINQ
            if (filteredWeight > _sessionMaxY)
            {
                _sessionMaxY = filteredWeight;
            }
        }

        // Bezpieczny limit punktów trzymanych w pamięci wykresu
        int maxCapacity = 4000;
        while (_leftValues.Count > maxCapacity) _leftValues.RemoveAt(0);
        while (_rightValues.Count > maxCapacity) _rightValues.RemoveAt(0);

        while (_leftBuffer.Count > BufferSize) _leftBuffer.RemoveAt(0);
        while (_rightBuffer.Count > BufferSize) _rightBuffer.RemoveAt(0);

        // --- AKTUALIZACJA OSI Y NA BAZIE ZAPISANEGO MAX sesji + mały margines ---
        _sharedYAxis.MaxLimit = _sessionMaxY < 5.0 ? 5.0 : _sessionMaxY * 1.05;

        // --- MANIPULACJA OSIAMI I SUWAKAMI ---
        ProcessAxisTick(_leftValues.Count, _leftXAxis, LeftScrollBar, ref _leftIsHistoryMode);
        ProcessAxisTick(_rightValues.Count, _rightXAxis, RightScrollBar, ref _rightIsHistoryMode);

        // Wyliczanie statystyk biomechanicznych
        _updateCounter++;
        if (_updateCounter >= 10 && (_leftBuffer.Count > 0 || _rightBuffer.Count > 0))
        {
            _updateCounter = 0;
            var result = BiomechanicsService.Calculate(_leftBuffer, _rightBuffer);
            UpdateStats(result);
        }
    }

    private void ProcessAxisTick(int totalPoints, Axis axis, ScrollBar scrollBar, ref bool isHistoryMode)
    {
        if (totalPoints <= ViewWindowSize) return;

        double maxScrollValue = totalPoints - ViewWindowSize;
        scrollBar.Maximum = maxScrollValue;
        scrollBar.ViewportSize = ViewWindowSize;

        if (axis.MaxLimit < (totalPoints - 3))
        {
            isHistoryMode = true;
        }

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
            scrollBar.Value = axis.MinLimit ?? 0;
        }
    }

    private void CheckIfReturnToLive(Axis axis, ref bool isHistoryMode, int totalPoints)
    {
        if (totalPoints <= ViewWindowSize) return;

        if (axis.MaxLimit >= (totalPoints - 5))
        {
            isHistoryMode = false;
            StatusText.Text = "Status: Pobieranie danych z platform...";
            StatusText.Foreground = Brushes.Green;
        }
        else
        {
            isHistoryMode = true;
            StatusText.Text = "Status: Przeglądanie danych historycznych sesji.";
            StatusText.Foreground = Brushes.Cyan;
        }
    }

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
        _lastCalculatedResult = r;
    }
}