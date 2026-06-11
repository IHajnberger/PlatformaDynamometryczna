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
using System.Linq;
using System.Threading.Tasks;

namespace UI_Test_Avalonia;

public partial class DataView : UserControl, IDisposable
{
    public event EventHandler? BackClicked;
    private readonly DispatcherTimer _renderTimer;
    private bool _isConnected;
    private bool _isRecording = false;

    private readonly ObservableCollection<double> _leftValues = new();
    private readonly ObservableCollection<double> _rightValues = new();

    private readonly List<(double Weight, DateTime Timestamp)> _leftBuffer = new();
    private readonly List<(double Weight, DateTime Timestamp)> _rightBuffer = new();
    private const int BufferSize = 200;
    private int _updateCounter = 0;
    private BiomechanicsResult _lastResult = new();
    public ObservableCollection<ISeries> LeftChartSeries { get; set; }
    public ObservableCollection<ISeries> RightChartSeries { get; set; }

    public Axis[] XAxesLeft { get; set; }
    public Axis[] XAxesRight { get; set; }
    public Axis[] YAxes { get; set; }
    private readonly Axis _yAxis; 

    public SolidColorPaint LegendPaint { get; set; } = new(SKColors.White);

    private readonly Axis _leftXAxis;
    private readonly Axis _rightXAxis;
    private const int ViewWindowSize = 200;

    private bool _leftIsHistoryMode = false;
    private bool _rightIsHistoryMode = false;
    private bool _isUpdatingFromScroll = false;

    public DataView()
    {
        InitializeComponent();
        Debug.WriteLine("[DataView] Constructor called.");

        BackButton.Click += (sender, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        StartRecordingButton.Click += (s, e) =>
        {
            _isRecording = true;
            _leftValues.Clear();
            _rightValues.Clear();
            _leftBuffer.Clear();
            _rightBuffer.Clear();
            
            // Reset Y-axis to the default 0-200 range on new recording
            _yAxis.MaxLimit = 200;
            _yAxis.MinLimit = 0;

            StartRecordingButton.IsVisible = false;
            StopRecordingButton.IsVisible = true;
            StatusText.Text = "Status: Nagrywanie...";
            StatusText.Foreground = Brushes.Red;
        };

        StopRecordingButton.Click += (s, e) =>
        {
            _isRecording = false;
            StartRecordingButton.IsVisible = true;
            StopRecordingButton.IsVisible = false;
            StatusText.Text = "Status: Nagrywanie zatrzymane.";
            StatusText.Foreground = Brushes.Cyan;
        };

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
                ExerciseName = "Przysiad (SQ)",
                Date = DateTime.Now,
                PeakForceLeft = _lastResult.PeakForceLeft,
                PeakForceRight = _lastResult.PeakForceRight,
                PeakForceTotal = _lastResult.PeakForceTotal,
                MeanForceLeft = _lastResult.MeanForceLeft,
                MeanForceRight = _lastResult.MeanForceRight,
                AsymmetryIndex = _lastResult.AsymmetryIndex,
                LoadRatioLeft = _lastResult.LoadRatioLeft,
                LoadRatioRight = _lastResult.LoadRatioRight,
                MinForceLeft = _lastResult.MinForceLeft,
                MinForceRight = _lastResult.MinForceRight,
                RFD = _lastResult.BrakingRFD
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
                AnimationsSpeed = TimeSpan.FromMilliseconds(0),
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
                AnimationsSpeed = TimeSpan.FromMilliseconds(0),
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

        // Initialize the Y-axis with the default range
        _yAxis = new Axis
        {
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
            TextSize = 11,
            Padding = new LiveChartsCore.Drawing.Padding(0, 0, 10, 0),
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1 },
            MinLimit = 0,
            MaxLimit = 200
        };
        YAxes = new Axis[] { _yAxis };

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

        if (!_isRecording)
        {
            if (isCurrentlyConnected != _isConnected)
            {
                _isConnected = isCurrentlyConnected;
                StatusText.Text = _isConnected ? "Status: Gotowy do nagrywania." : "Status: Połączenie przerwane. Oczekiwanie na ESP32...";
                StatusText.Foreground = _isConnected ? Brushes.Green : Brushes.Orange;
            }
            while (MqttService.Instance.Device1Queue.TryDequeue(out _)) { }
            while (MqttService.Instance.Device2Queue.TryDequeue(out _)) { }
            return;
        }

        // --- Recording Logic ---
        int itemsProcessed = 0;
        int maxItemsToProcessPerTick = 200; 

        double newMax = _yAxis.MaxLimit ?? 200;
        bool maxChanged = false;

        while (itemsProcessed < maxItemsToProcessPerTick && MqttService.Instance.Device1Queue.TryDequeue(out var dataL))
        {
            _leftValues.Add(dataL.Weight);
            _leftBuffer.Add(dataL);
            if (dataL.Weight > newMax)
            {
                newMax = dataL.Weight;
                maxChanged = true;
            }

            if (MqttService.Instance.Device2Queue.TryDequeue(out var dataR))
            {
                _rightValues.Add(dataR.Weight);
                _rightBuffer.Add(dataR);
                if (dataR.Weight > newMax)
                {
                    newMax = dataR.Weight;
                    maxChanged = true;
                }
            }
            itemsProcessed++;
        }

        if (maxChanged)
        {
            // Set the new max limit with a 10% buffer
            _yAxis.MaxLimit = newMax * 1.1;
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
            if (_isRecording)
            {
                StatusText.Text = "Status: Nagrywanie...";
                StatusText.Foreground = Brushes.Red;
            }
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

            // Dół przysiadu = minimalna siła lewej nogi (moment najgłębszego ugięcia)
            FlightTimeText.Text = $"{r.MinForceLeft:F1} kg";

            // RFD przy wstawaniu
            BrakingRFDText.Text = $"{r.BrakingRFD:F1} kg/s";

            SummaryPeakText.Text = $"{r.PeakForceTotal:F1} kg";
            SummaryMeanText.Text = $"{r.MeanForceTotal:F1} kg";
            SummaryMinText.Text = $"{Math.Min(r.MinForceLeft, r.MinForceRight):F1} kg";
            SummaryAsymmetryText.Text = $"{r.AsymmetryIndex:F1}%";

            BalanceLeftText.Text = $"L: {r.LoadRatioLeft:F1}%";
            BalanceRightText.Text = $"R: {r.LoadRatioRight:F1}%";

            // Kolorowanie asymetrii – zielony gdy OK, czerwony gdy >10%
            AsymmetryText.Foreground = Math.Abs(r.AsymmetryIndex) > 10
                ? Brushes.Red
                : SolidColorBrush.Parse("#10b981");
            SummaryAsymmetryText.Foreground = AsymmetryText.Foreground;
            _lastResult = r;
        }, DispatcherPriority.Render);
    }
}