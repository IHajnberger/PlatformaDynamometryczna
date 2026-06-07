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
using System.Threading.Tasks;

namespace UI_Test_Avalonia;

public partial class DataView : UserControl, IDisposable
{
    public event EventHandler? BackClicked;
    private readonly DispatcherTimer _renderTimer;
    private bool _isConnected;
    
    private readonly ObservableCollection<double> _leftValues = new();
    private readonly ObservableCollection<double> _rightValues = new();

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
    private const int ViewWindowSize = 100; 

    private bool _leftIsHistoryMode = false;
    private bool _rightIsHistoryMode = false;
    private bool _isUpdatingFromScroll = false;

    public DataView()
    {
        InitializeComponent();
        Debug.WriteLine("[DataView] Constructor called.");

        BackButton.Click += (sender, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        SaveSessionButton.Click += (s, e) =>
        {
            var activePatient = PatientService.Instance.ActivePatient;
            if (activePatient == null)
            {
                StatusText.Text = "Status: Wybierz pacjenta przed zapisem sesji!";
                StatusText.Foreground = Brushes.Orange;
                return;
            }

            SessionService.Instance.AddSession(new Session
            {
                PatientId = activePatient.Id,
                ExerciseName = "Skok pionowy",
                Date = DateTime.Now
            });

            StatusText.Text = $"Status: Sesja zapisana dla {activePatient.FullName}!";
            StatusText.Foreground = Brushes.Green;
        };

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
                AnimationsSpeed = TimeSpan.FromMilliseconds(0), // Disable animations for live data
                EasingFunction = null
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
                AnimationsSpeed = TimeSpan.FromMilliseconds(0), // Disable animations for live data
                EasingFunction = null
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

        // We process all available items, but limit it to prevent UI freezing
        // if there's a massive backlog.
        int itemsProcessed = 0;
        int maxItemsToProcessPerTick = 100;

        while (itemsProcessed < maxItemsToProcessPerTick && MqttService.Instance.Device1Queue.TryDequeue(out var dataL))
        {
            _leftValues.Add(dataL.Weight);
            _leftBuffer.Add(dataL);
            
            if (MqttService.Instance.Device2Queue.TryDequeue(out var dataR))
            {
                 _rightValues.Add(dataR.Weight);
                 _rightBuffer.Add(dataR);
            }
            itemsProcessed++;
        }


        int maxCapacity = 4000;
        while (_leftValues.Count > maxCapacity) _leftValues.RemoveAt(0);
        while (_rightValues.Count > maxCapacity) _rightValues.RemoveAt(0);

        while (_leftBuffer.Count > BufferSize) _leftBuffer.RemoveAt(0);
        while (_rightBuffer.Count > BufferSize) _rightBuffer.RemoveAt(0);

        ProcessAxisTick(_leftValues.Count, _leftXAxis, LeftScrollBar, ref _leftIsHistoryMode);
        ProcessAxisTick(_rightValues.Count, _rightXAxis, RightScrollBar, ref _rightIsHistoryMode);

        _updateCounter++;
        if (_updateCounter >= 10 && (_leftBuffer.Count > 0 || _rightBuffer.Count > 0))
        {
            _updateCounter = 0;
            
            var leftBufferCopy = new List<(double Weight, DateTime Timestamp)>(_leftBuffer);
            var rightBufferCopy = new List<(double Weight, DateTime Timestamp)>(_rightBuffer);

            Task.Run(() =>
            {
                var result = BiomechanicsService.Calculate(leftBufferCopy, rightBufferCopy);
                UpdateStats(result); 
            });
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
            _isWindowUpdating = false;
            _isUpdatingFromScroll = false;
        }
        else if (!_isUpdatingFromScroll)
        {
            scrollBar.Value = axis.MinLimit ?? 0;
        }
    }
    
    private bool _isWindowUpdating = false;

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
    }
}