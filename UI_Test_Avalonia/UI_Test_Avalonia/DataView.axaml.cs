using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
using System.Linq; // DODANE: Wymagane do skalowania (metoda .Max())

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
    
    // ZMIANA 1: Zwiększone okno czasowe (np. 400 próbek * 12ms = 4.8 sekundy podążania)
    private const int ViewWindowSize = 400; 

    private bool _leftIsHistoryMode = false;
    private bool _rightIsHistoryMode = false;
    private bool _isUpdatingFromScroll = false;

    // Słownik:
    private Dictionary<ExerciseParam, Border> _paramBorders = new();

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
                ExerciseName = ExerciseService.Instance.ActiveExercise?.Name ?? "Nieznane",
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
                AnimationsSpeed = TimeSpan.Zero, 
                GeometryFill = new SolidColorPaint(leftColor),
                GeometryStroke = new SolidColorPaint(leftColor) { StrokeThickness = 2 },
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
                AnimationsSpeed = TimeSpan.Zero, 
                GeometryFill = new SolidColorPaint(rightColor),
                GeometryStroke = new SolidColorPaint(rightColor) { StrokeThickness = 2 },
                EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
            }
        };
        
        _leftXAxis = new Axis
        {
            TextSize = 11, // Włączony tekst
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
            Labeler = value => (value * 0.012).ToString("F1") , 
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1, PathEffect = new DashEffect(new float[] { 4, 4 }) },
            MinLimit = 0,
            MaxLimit = ViewWindowSize,
            MinStep = 1000.0 / 12.0
        };

        _rightXAxis = new Axis
        {
            TextSize = 11,
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
            Labeler = value => (value * 0.012).ToString("F1"),
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1, PathEffect = new DashEffect(new float[] { 4, 4 }) },
            MinLimit = 0,
            MaxLimit = ViewWindowSize,
            MinStep = 1000.0 / 12.0
        };

        XAxesLeft = new Axis[] { _leftXAxis };
        XAxesRight = new Axis[] { _rightXAxis };

        // ZMIANA 3: Twardy początkowy limit Y = 50
        YAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                MaxLimit = 50, // Minimalny "dach" to 50
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

        UpdateSessionInfo();
        PatientService.Instance.ActivePatientChanged += (s, e) => Dispatcher.UIThread.Post(UpdateSessionInfo);
        ExerciseService.Instance.ActiveExerciseChanged += (s, e) => Dispatcher.UIThread.Post(UpdateSessionInfo);
        
        _paramBorders = new Dictionary<ExerciseParam, Border>
        {
            { ExerciseParam.PeakForceL,       ParamPeakForceL },
            { ExerciseParam.PeakForceR,       ParamPeakForceR },
            { ExerciseParam.MeanForceL,       ParamMeanForceL },
            { ExerciseParam.MeanForceR,       ParamMeanForceR },
            { ExerciseParam.LoadRatio,        ParamLoadRatio },
            { ExerciseParam.AsymmetryIndex,   ParamAsymmetry },
            { ExerciseParam.RFD,              ParamRFD },
            { ExerciseParam.TotalForce,       ParamTotalForce },
            { ExerciseParam.StabilityIndex,   ParamStabilityIndex },
            { ExerciseParam.SwayVelocity,     ParamSwayVelocity  },
            { ExerciseParam.ForceVariability, ParamForceVariability },
            { ExerciseParam.TimeToPeakForce,  ParamTimeToPeak },
            { ExerciseParam.StabilizationTime, ParamStabilizationTime },
            { ExerciseParam.WeightTransferSpeed, ParamTransferSpd },
            { ExerciseParam.FatigueIndex,     ParamFatigueIndex },
            { ExerciseParam.ControlScore,     ParamControlScore },
        };

        ExerciseService.Instance.ActiveExerciseChanged += (s, e) =>
            Dispatcher.UIThread.Post(ApplyExerciseFilter);

        ApplyExerciseFilter();

        _renderTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _renderTimer.Tick += RenderTimer_Tick;

        DetachedFromVisualTree += (_, _) => _renderTimer.Stop();

        StopSessionButton.Content = new Avalonia.Controls.StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new FluentAvalonia.UI.Controls.SymbolIcon { Symbol = FluentAvalonia.UI.Controls.Symbol.Play, FontSize = 16, Foreground = Brushes.LightGreen },
                new Avalonia.Controls.TextBlock { Text = "Start", Foreground = Brushes.White, FontWeight = Avalonia.Media.FontWeight.Bold }
            }
        };

        StopSessionButton.Click += (s, e) =>
        {
            if (_renderTimer.IsEnabled)
            {
                _renderTimer.Stop();
                StopSessionButton.Content = new Avalonia.Controls.StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new FluentAvalonia.UI.Controls.SymbolIcon { Symbol = FluentAvalonia.UI.Controls.Symbol.Play, FontSize = 16, Foreground = Brushes.LightGreen },
                        new Avalonia.Controls.TextBlock { Text = "Start", Foreground = Brushes.White, FontWeight = Avalonia.Media.FontWeight.Bold }
                    }
                };
            }
            else
            {
                _leftValues.Clear();
                _rightValues.Clear();
                _leftBuffer.Clear();
                _rightBuffer.Clear();

                MqttService.Instance.Device1Queue.Clear();
                MqttService.Instance.Device2Queue.Clear();

                _leftXAxis.MinLimit = 0;
                _leftXAxis.MaxLimit = ViewWindowSize;
                _rightXAxis.MinLimit = 0;
                _rightXAxis.MaxLimit = ViewWindowSize;
                
                YAxes[0].MaxLimit = 50; // Reset Y axis

                _renderTimer.Start();
                StopSessionButton.Content = new Avalonia.Controls.StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new FluentAvalonia.UI.Controls.SymbolIcon { Symbol = FluentAvalonia.UI.Controls.Symbol.Stop, FontSize = 16, Foreground = Brushes.Orange },
                        new Avalonia.Controls.TextBlock { Text = "Stop", Foreground = Brushes.White, FontWeight = Avalonia.Media.FontWeight.Bold }
                    }
                };
            }
        };

        MarkPeakButton.Click += (s, e) => { };

        ClearAllButton.Click += (s, e) =>
        {
            _leftValues.Clear();
            _rightValues.Clear();
            _leftBuffer.Clear();
            _rightBuffer.Clear();
            
            MqttService.Instance.Device1Queue.Clear();
            MqttService.Instance.Device2Queue.Clear();
            
            _leftXAxis.MinLimit = 0;
            _leftXAxis.MaxLimit = ViewWindowSize;
            _rightXAxis.MinLimit = 0;
            _rightXAxis.MaxLimit = ViewWindowSize;
            
            YAxes[0].MaxLimit = 50; // Reset Y axis
        };
    }

    public void Dispose() => _renderTimer.Stop();

    private void ApplyExerciseFilter()
    {
        var exercise = ExerciseService.Instance.ActiveExercise;

        if (exercise == null)
        {
            foreach (var border in _paramBorders.Values)
                border.IsVisible = true;
            return;
        }

        foreach (var (param, border) in _paramBorders)
            border.IsVisible = exercise.Params.Contains(param);
    }

    private void UpdateSessionInfo()
    {
        var patient = PatientService.Instance.ActivePatient;
        var exercise = ExerciseService.Instance.ActiveExercise;

        SessionPatientText.Text = patient != null ? patient.FullName : "Nie wybrano";
        SessionExerciseText.Text = exercise != null ? exercise.Name : "Nie wybrano";

        SessionPatientText.Foreground = patient != null ? Brushes.White : Brushes.Orange;
        SessionExerciseText.Foreground = exercise != null ? Brushes.White : Brushes.Orange;
    }

    private void RenderTimer_Tick(object? sender, EventArgs e)
    {
        bool isCurrentlyConnected = (DateTime.Now - MqttService.Instance.LastPacketTime).TotalMilliseconds < 4000;

        if (isCurrentlyConnected != _isConnected)
        {
            _isConnected = isCurrentlyConnected;
            StatusText.Text = _isConnected ? "Status: Pobieranie danych z platform..." : "Status: Połączenie przerwane. Oczekiwanie na ESP32...";
            StatusText.Foreground = _isConnected ? Brushes.Green : Brushes.Orange;
        }

        int leftQueueCount = MqttService.Instance.Device1Queue.Count;
        int pointsToProcessL = 0;
        if (leftQueueCount > 200) pointsToProcessL = leftQueueCount / 10; 
        else if (leftQueueCount > 40) pointsToProcessL = 2; 
        else if (leftQueueCount > 0) pointsToProcessL = 1;  
        
        int pointsProcessedL = 0;
        for (int i = 0; i < pointsToProcessL && MqttService.Instance.Device1Queue.TryDequeue(out var data); i++)
        {
            _leftValues.Add(data.Weight);
            _leftBuffer.Add(data);
            pointsProcessedL++;
        }

        int rightQueueCount = MqttService.Instance.Device2Queue.Count;
        int pointsToProcessR = 0;
        if (rightQueueCount > 200) pointsToProcessR = rightQueueCount / 10;
        else if (rightQueueCount > 40) pointsToProcessR = 2;
        else if (rightQueueCount > 0) pointsToProcessR = 1;
        
        int pointsProcessedR = 0;
        for (int i = 0; i < pointsToProcessR && MqttService.Instance.Device2Queue.TryDequeue(out var data); i++)
        {
            _rightValues.Add(data.Weight);
            _rightBuffer.Add(data);
            pointsProcessedR++;
        }

        int maxCapacity = 4000;
        while (_leftValues.Count > maxCapacity) _leftValues.RemoveAt(0);
        while (_rightValues.Count > maxCapacity) _rightValues.RemoveAt(0);

        while (_leftBuffer.Count > BufferSize) _leftBuffer.RemoveAt(0);
        while (_rightBuffer.Count > BufferSize) _rightBuffer.RemoveAt(0);

        if (pointsProcessedL > 0)
            ProcessAxisTick(_leftValues.Count, _leftXAxis, LeftScrollBar, ref _leftIsHistoryMode);
            
        if (pointsProcessedR > 0)
            ProcessAxisTick(_rightValues.Count, _rightXAxis, RightScrollBar, ref _rightIsHistoryMode);

        _updateCounter++;
        if (_updateCounter >= 10 && (_leftBuffer.Count > 0 || _rightBuffer.Count > 0))
        {
            _updateCounter = 0;
            var result = BiomechanicsService.Calculate(_leftBuffer, _rightBuffer);
            UpdateStats(result);

            // ZMIANA 4: Dynamiczne wspólne skalowanie osi Y 
            // Liczone co 10 ticków (~5 razy na sekundę) aby odciążyć UI
            double currentMax = 50.0;
            if (_leftValues.Count > 0) currentMax = Math.Max(currentMax, _leftValues.Max());
            if (_rightValues.Count > 0) currentMax = Math.Max(currentMax, _rightValues.Max());

            // Skaluje do najwyższej znalezionej wartości + dodaje malutki margines (10%) u góry.
            YAxes[0].MaxLimit = currentMax > 50 ? currentMax * 1.1 : 50;
        }
    }

    private void ProcessAxisTick(int totalPoints, Axis axis, ScrollBar scrollBar, ref bool isHistoryMode)
    {
        if (totalPoints <= ViewWindowSize) return;

        double maxScrollValue = totalPoints - ViewWindowSize;
        scrollBar.Maximum = maxScrollValue;
        scrollBar.ViewportSize = ViewWindowSize;

        if (axis.MaxLimit < (totalPoints - 3))
            isHistoryMode = true;

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
            LoadRatioText.Text = $"L:{r.LoadRatioLeft:F1}%  R:{r.LoadRatioRight:F1}%";
            AsymmetryText.Text = $"{r.AsymmetryIndex:F1}%";
            RFDText.Text = $"{r.RFD:F1} kg/s";
            TotalForceText.Text = $"{r.TotalForce:F1} kg";
            StabilityIndexText.Text = $"{r.StabilityIndex:F2}";
            StabilizationTimeText.Text = $"{r.StabilizationTime:F2} s";
            TimeToPeakText.Text = $"{r.TimeToPeakForce:F2} s";
            TransferSpeedText.Text = $"{r.WeightTransferSpeed:F1} kg/s";
            SwayVelocityText.Text = $"{r.SwayVelocity:F2}";
            ForceVariabilityText.Text = $"{r.ForceVariability:F2}";
            ControlScoreText.Text = $"{r.ControlScore:F1}%";
            FatigueIndexText.Text = $"{r.FatigueIndex:F1}%";

            SummaryPeakText.Text = $"{r.PeakForceTotal:F1} kg";
            SummaryMeanText.Text = $"{r.MeanForceTotal:F1} kg";
            SummaryMinText.Text = $"{Math.Min(r.MinForceLeft, r.MinForceRight):F1} kg";
            SummaryAsymmetryText.Text = $"{r.AsymmetryIndex:F1}%";
        }, DispatcherPriority.Render);
    }
}