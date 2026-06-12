using Avalonia.Controls;
using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UI_Test_Avalonia;

public partial class SessionDetailView : UserControl
{
    public event EventHandler? BackClicked;

    public ObservableCollection<ISeries> LeftChartSeries { get; set; }
    public ObservableCollection<ISeries> RightChartSeries { get; set; }

    public Axis[] XAxesLeft { get; set; }
    public Axis[] XAxesRight { get; set; }
    public Axis[] YAxes { get; set; }
    public SolidColorPaint LegendPaint { get; set; } = new(SKColors.White);

    private readonly Axis _leftXAxis;
    private readonly Axis _rightXAxis;
    
    private Session _session;
    private Dictionary<ExerciseParam, Border> _paramBorders = new();

    public SessionDetailView(Session session)
    {
        InitializeComponent();
        _session = session;

        BackButton.Click += (sender, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        DateText.Text = $"Data: {session.Date:dd.MM.yyyy HH:mm}";
        ExerciseText.Text = session.ExerciseName;
        SummaryPeakText.Text = $"{session.PeakForceTotal:F1} kg";
        SummaryMeanText.Text = $"{session.MeanForceTotal:F1} kg";
        SummaryAsymmetryText.Text = $"{session.AsymmetryIndex:F1}%";

        var leftColor = SKColor.Parse("#3b82f6");
        var rightColor = SKColor.Parse("#f59e0b");

        LeftChartSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<double>
            {
                Name = "Left Scale",
                Values = new ObservableCollection<double>(session.LeftChartData ?? new()),
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
                Values = new ObservableCollection<double>(session.RightChartData ?? new()),
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
        
        // Remove MinLimit and MaxLimit to let LiveCharts auto-scale to the full session
        // This makes ZoomMode="X" work perfectly.
        _leftXAxis = new Axis
        {
            TextSize = 11,
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
            Labeler = value => (value * 0.012).ToString("F1") , 
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333").WithAlpha(35)) { StrokeThickness = 1, PathEffect = new DashEffect(new float[] { 4, 4 }) },
            MinStep = 1000.0 / 12.0
        };

        _rightXAxis = new Axis
        {
            TextSize = 11,
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
            Labeler = value => (value * 0.012).ToString("F1"),
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333").WithAlpha(35)) { StrokeThickness = 1, PathEffect = new DashEffect(new float[] { 4, 4 }) },
            MinStep = 1000.0 / 12.0
        };

        XAxesLeft = new Axis[] { _leftXAxis };
        XAxesRight = new Axis[] { _rightXAxis };

        double maxLeft = session.LeftChartData?.Count > 0 ? session.LeftChartData.Max() : 50;
        double maxRight = session.RightChartData?.Count > 0 ? session.RightChartData.Max() : 50;
        double currentMax = Math.Max(maxLeft, maxRight);

        YAxes = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                MaxLimit = currentMax > 50 ? currentMax * 1.1 : 50,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
                TextSize = 11,
                Padding = new LiveChartsCore.Drawing.Padding(0, 0, 10, 0),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333").WithAlpha(35)) { StrokeThickness = 1 }
            }
        };

        DataContext = this;
        
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
        
        PopulateStats(session);
        ApplyExerciseFilter(session);
    }

    private void PopulateStats(Session session)
    {
        PeakForceLText.Text = $"{session.PeakForceLeft:F1} kg";
        PeakForceRText.Text = $"{session.PeakForceRight:F1} kg";
        MeanForceLText.Text = $"{session.MeanForceLeft:F1} kg";
        MeanForceRText.Text = $"{session.MeanForceRight:F1} kg";
        LoadRatioText.Text = $"L:{session.LoadRatioLeft:F1}%  R:{session.LoadRatioRight:F1}%";
        AsymmetryText.Text = $"{session.AsymmetryIndex:F1}%";
        RFDText.Text = $"{session.RFD:F1} kg/s";
        TotalForceText.Text = $"{session.TotalForce:F1} kg";
        StabilityIndexText.Text = $"{session.StabilityIndex:F2}";
        StabilizationTimeText.Text = $"{session.StabilizationTime:F2} s";
        TimeToPeakText.Text = $"{session.TimeToPeakForce:F2} s";
        TransferSpeedText.Text = $"{session.WeightTransferSpeed:F1} kg/s";
        SwayVelocityText.Text = $"{session.SwayVelocity:F2}";
        ForceVariabilityText.Text = $"{session.ForceVariability:F2}";
        ControlScoreText.Text = $"{session.ControlScore:F1}%";
        FatigueIndexText.Text = $"{session.FatigueIndex:F1}%";
    }

    private void ApplyExerciseFilter(Session session)
    {
        var exercise = ExerciseService.Instance.Exercises.FirstOrDefault(e => e.Name == session.ExerciseName);

        if (exercise == null)
        {
            foreach (var border in _paramBorders.Values)
                border.IsVisible = true;
            return;
        }

        foreach (var (param, border) in _paramBorders)
            border.IsVisible = exercise.Params.Contains(param);
    }
}