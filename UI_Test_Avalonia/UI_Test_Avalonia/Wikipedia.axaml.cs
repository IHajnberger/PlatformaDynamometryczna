using System;
using Avalonia.Controls;

namespace UI_Test_Avalonia;

public partial class Wikipedia : UserControl
{
    public event EventHandler? BackClicked;
    public event EventHandler<string>? ExerciseSelected;

    public Wikipedia()
    {
        InitializeComponent();

        BackButton.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        /* old
        TileSQ.Click += (s, e) => ExerciseSelected?.Invoke(this, "SQ");
        TileISO.Click += (s, e) => ExerciseSelected?.Invoke(this, "ISO");
        */
        TileSRCL.Click += (s, e) => ExerciseSelected?.Invoke(this, "SRC");
        TilePJL.Click += (s, e) => ExerciseSelected?.Invoke(this, "PJL");
        TilePJP.Click += (s, e) => ExerciseSelected?.Invoke(this, "PJP");
        TileTWiS.Click += (s, e) => ExerciseSelected?.Invoke(this, "TWiS");
        TilePO.Click += (s, e) => ExerciseSelected?.Invoke(this, "PO");
        TilePR.Click += (s, e) => ExerciseSelected?.Invoke(this, "PR");
        TileTWiI.Click += (s, e) => ExerciseSelected?.Invoke(this, "TWiI");
    }
}
/*
do dodania:
new Exercise
        {
            Name = "Statyczny rozkład ciężaru",
            Description = "Static Weight Distribution",
            Params = new() { ExerciseParam.PeakForceL, ExerciseParam.PeakForceR, ExerciseParam.MeanForceL, ExerciseParam.MeanForceR, ExerciseParam.LoadRatio, ExerciseParam.AsymmetryIndex, ExerciseParam.TotalForce }
        },
        new Exercise
        {
            Name = "Próba jednonożna L",
            Description = "Single Leg Stance Left",
            Params = new() { ExerciseParam.MeanForceL, ExerciseParam.MeanForceR, ExerciseParam.LoadRatio, ExerciseParam.AsymmetryIndex, ExerciseParam.StabilityIndex, ExerciseParam.SwayVelocity, ExerciseParam.ForceVariability, ExerciseParam.StabilizationTime }
        },
        new Exercise
        {
            Name = "Próba jednonożna P",
            Description = "Single Leg Stance Right",
            Params = new() { ExerciseParam.MeanForceL, ExerciseParam.MeanForceR, ExerciseParam.LoadRatio, ExerciseParam.AsymmetryIndex, ExerciseParam.StabilityIndex, ExerciseParam.SwayVelocity, ExerciseParam.ForceVariability, ExerciseParam.StabilizationTime }
        },
        new Exercise
        {
            Name = "Test wstawania i siadania",
            Description = "Sit To Stand",
            Params = new() { ExerciseParam.PeakForceL, ExerciseParam.PeakForceR, ExerciseParam.MeanForceL, ExerciseParam.MeanForceR, ExerciseParam.AsymmetryIndex, ExerciseParam.RFD, ExerciseParam.TimeToPeakForce, ExerciseParam.WeightTransferSpeed, ExerciseParam.FatigueIndex }
        },
        new Exercise
        {
            Name = "Przysiad obustronny",
            Description = "Squat Assessment",
            Params = new() { ExerciseParam.PeakForceL, ExerciseParam.PeakForceR, ExerciseParam.MeanForceL, ExerciseParam.MeanForceR, ExerciseParam.MinForceL, ExerciseParam.MinForceR, ExerciseParam.AsymmetryIndex, ExerciseParam.LoadRatio, ExerciseParam.RFD, ExerciseParam.TimeToPeakForce }
        },
        new Exercise
        {
            Name = "Próba Romberga",
            Description = "Balance Test (Romberg)",
            Params = new() { ExerciseParam.MeanForceL, ExerciseParam.MeanForceR, ExerciseParam.LoadRatio, ExerciseParam.StabilityIndex, ExerciseParam.SwayVelocity, ExerciseParam.ForceVariability, ExerciseParam.ControlScore }
        },
        new Exercise
        {
            Name = "Test wytrzymałości izometrycznej",
            Description = "Isometric Strength Test",
            Params = new() { ExerciseParam.PeakForceL, ExerciseParam.PeakForceR, ExerciseParam.MeanForceL, ExerciseParam.MeanForceR, ExerciseParam.RFD, ExerciseParam.FatigueIndex, ExerciseParam.TimeToPeakForce, ExerciseParam.ControlScore }
        },
*/