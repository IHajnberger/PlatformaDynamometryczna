using System;
using System.Collections.Generic;

namespace UI_Test_Avalonia;

public enum ExerciseParam
{
    PeakForceL, PeakForceR,
    MeanForceL, MeanForceR,
    MinForceL, MinForceR,
    LoadRatio, AsymmetryIndex,
    RFD, StabilityIndex, SwayVelocity, ForceVariability,
    TimeToPeakForce, StabilizationTime, WeightTransferSpeed,
    TotalForce, ControlScore, FatigueIndex
}

public class Exercise
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public HashSet<ExerciseParam> Params { get; set; } = new();
}

public sealed class ExerciseService
{
    private static readonly Lazy<ExerciseService> lazy = new(() => new ExerciseService());
    public static ExerciseService Instance => lazy.Value;

    public Exercise? ActiveExercise { get; private set; }
    public event EventHandler? ActiveExerciseChanged;

    public Exercise[] Exercises { get; } = new[]
    {
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
    };

    private ExerciseService() { }

    public void SetActiveExercise(Exercise? exercise)
    {
        ActiveExercise = exercise;
        ActiveExerciseChanged?.Invoke(this, EventArgs.Empty);
    }
}