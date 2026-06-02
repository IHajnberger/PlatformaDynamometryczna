using System;
using System.Collections.Generic;
using System.Linq;

namespace UI_Test_Avalonia;

/*
System obliczający biomechaniczne wskaźniki z danych siłowych z platformy.
Wersja bezpieczna - odporna na brak sygnału z jednej z platform (Single-Device Fallback).
*/
public class BiomechanicsResult
{
    // Siły podstawowe
    public double PeakForceLeft { get; set; }
    public double PeakForceRight { get; set; }
    public double PeakForceTotal { get; set; }

    public double MeanForceLeft { get; set; }
    public double MeanForceRight { get; set; }
    public double MeanForceTotal { get; set; }

    public double MinForceLeft { get; set; }
    public double MinForceRight { get; set; }

    // Rozkład obciążenia
    public double LoadRatioLeft { get; set; }
    public double LoadRatioRight { get; set; }

    public double AsymmetryIndex { get; set; }

    // Dynamika
    public double RFD { get; set; }

    // Stabilność
    public double StabilityIndex { get; set; }
    public double SwayVelocity { get; set; }
    public double ForceVariability { get; set; }

    // Kontrola ruchu
    public double TimeToPeakForce { get; set; }
    public double StabilizationTime { get; set; }
    public double WeightTransferSpeed { get; set; }

    // Wyniki zbiorcze
    public double TotalForce { get; set; }
    public double ControlScore { get; set; }
    public double FatigueIndex { get; set; }
}
public static class BiomechanicsService
{
    private const double FlightThreshold = 2.0; // kg — poniżej tej sumy = w powietrzu

    public static BiomechanicsResult Calculate(
        IReadOnlyList<(double Weight, DateTime Timestamp)> leftSamples,
        IReadOnlyList<(double Weight, DateTime Timestamp)> rightSamples)
    {
        var result = new BiomechanicsResult();

        // 1. Zabezpieczenie: Jeśli obie platformy są puste, nie ma sensu wykonywać obliczeń
        if (leftSamples.Count == 0 && rightSamples.Count == 0)
            return result;

        // 2. Bezpieczne Peak Force - sprawdzamy strukturę przy użyciu operatora trójargumentowego (? :)
        result.PeakForceLeft = leftSamples.Count > 0 ? leftSamples.Max(s => s.Weight) : 0.0;
        result.PeakForceRight = rightSamples.Count > 0 ? rightSamples.Max(s => s.Weight) : 0.0;
        result.PeakForceTotal = result.PeakForceLeft + result.PeakForceRight;

        // 3. Bezpieczne Min Force
        result.MinForceLeft = leftSamples.Count > 0 ? leftSamples.Min(s => s.Weight) : 0.0;
        result.MinForceRight = rightSamples.Count > 0 ? rightSamples.Min(s => s.Weight) : 0.0;

        // 4. Bezpieczne Mean Force
        result.MeanForceLeft = leftSamples.Count > 0 ? leftSamples.Average(s => s.Weight) : 0.0;
        result.MeanForceRight = rightSamples.Count > 0 ? rightSamples.Average(s => s.Weight) : 0.0;
        result.MeanForceTotal = result.MeanForceLeft + result.MeanForceRight;

        // 5. Load Ratio (Tylko gdy sumaryczna masa jest większa od zera)
        double totalMean = result.MeanForceLeft + result.MeanForceRight;
        if (totalMean > 0)
        {
            result.LoadRatioLeft = (result.MeanForceLeft / totalMean) * 100.0;
            result.LoadRatioRight = (result.MeanForceRight / totalMean) * 100.0;
        }

        // 6. Asymmetry Index (Limb Symmetry Index)
        double totalPeak = result.PeakForceLeft + result.PeakForceRight;
        if (totalPeak > 0)
        {
            result.AsymmetryIndex = ((result.PeakForceLeft - result.PeakForceRight) / totalPeak) * 100.0;
        }

        // 7. Dynamiczne parametry zaawansowane (Wyliczamy TYLKO wtedy, gdy podłączone są obydwie platformy)
        if (leftSamples.Count > 0 && rightSamples.Count > 0)
        {
           
            result.RFD = CalculateRFD(leftSamples, rightSamples);
        }
        else
        {
            // Jeśli działa tylko jedna platforma, wskaźniki dynamiczne (czas lotu, RFD) tracą sens biomechaniczny
            result.RFD = 0.0;
        }

        result.TotalForce = result.MeanForceTotal;

        result.StabilityIndex = CalculateStabilityIndex(leftSamples, rightSamples);

        result.SwayVelocity = result.StabilityIndex;

        result.ForceVariability =
            (CalculateStdDev(leftSamples.Select(x => x.Weight)) +
             CalculateStdDev(rightSamples.Select(x => x.Weight))) / 2.0;

        result.TimeToPeakForce =
            CalculateTimeToPeak(leftSamples, rightSamples);

        result.StabilizationTime =
            CalculateStabilizationTime(leftSamples, rightSamples);

        result.WeightTransferSpeed =
            CalculateWeightTransferSpeed(leftSamples, rightSamples);

        result.ControlScore =
            Math.Max(0, 100 - Math.Abs(result.AsymmetryIndex));

        result.FatigueIndex =
            CalculateFatigueIndex(leftSamples, rightSamples);

        return result;
    }

    private static double CalculateStabilityIndex(
    IReadOnlyList<(double Weight, DateTime Timestamp)> left,
    IReadOnlyList<(double Weight, DateTime Timestamp)> right)
    {
        var values = left.Select(x => x.Weight)
            .Concat(right.Select(x => x.Weight))
            .ToList();

        if (values.Count < 2)
            return 0;

        return CalculateStdDev(values);
    }

    private static double CalculateStdDev(IEnumerable<double> values)
    {
        var list = values.ToList();

        if (list.Count < 2)
            return 0;

        double avg = list.Average();

        double variance =
            list.Sum(v => Math.Pow(v - avg, 2)) / list.Count;

        return Math.Sqrt(variance);
    }

    private static double CalculateTimeToPeak(
        IReadOnlyList<(double Weight, DateTime Timestamp)> left,
        IReadOnlyList<(double Weight, DateTime Timestamp)> right)
    {
        int count = Math.Min(left.Count, right.Count);

        if (count < 2)
            return 0;

        double peak = double.MinValue;
        DateTime peakTime = left[0].Timestamp;

        for (int i = 0; i < count; i++)
        {
            double total = left[i].Weight + right[i].Weight;

            if (total > peak)
            {
                peak = total;
                peakTime = left[i].Timestamp;
            }
        }

        return (peakTime - left[0].Timestamp).TotalSeconds;
    }

    private static double CalculateStabilizationTime(
        IReadOnlyList<(double Weight, DateTime Timestamp)> left,
        IReadOnlyList<(double Weight, DateTime Timestamp)> right)
    {
        if (left.Count < 10 || right.Count < 10)
            return 0;

        return (left.Last().Timestamp - left.First().Timestamp)
            .TotalSeconds;
    }

    private static double CalculateWeightTransferSpeed(
        IReadOnlyList<(double Weight, DateTime Timestamp)> left,
        IReadOnlyList<(double Weight, DateTime Timestamp)> right)
    {
        int count = Math.Min(left.Count, right.Count);

        if (count < 2)
            return 0;

        double maxTransfer = 0;

        for (int i = 1; i < count; i++)
        {
            double prevDiff =
                Math.Abs(left[i - 1].Weight - right[i - 1].Weight);

            double currDiff =
                Math.Abs(left[i].Weight - right[i].Weight);

            double dt =
                (left[i].Timestamp - left[i - 1].Timestamp)
                .TotalSeconds;

            if (dt <= 0)
                continue;

            double speed =
                Math.Abs(currDiff - prevDiff) / dt;

            if (speed > maxTransfer)
                maxTransfer = speed;
        }

        return maxTransfer;
    }

    private static double CalculateFatigueIndex(
        IReadOnlyList<(double Weight, DateTime Timestamp)> left,
        IReadOnlyList<(double Weight, DateTime Timestamp)> right)
    {
        int count = Math.Min(left.Count, right.Count);

        if (count < 20)
            return 0;

        int half = count / 2;

        double firstHalf =
            Enumerable.Range(0, half)
            .Average(i => left[i].Weight + right[i].Weight);

        double secondHalf =
            Enumerable.Range(half, count - half)
            .Average(i => left[i].Weight + right[i].Weight);

        if (firstHalf <= 0)
            return 0;

        return ((firstHalf - secondHalf) / firstHalf) * 100.0;
    }

    private static double CalculateRFD(
        IReadOnlyList<(double Weight, DateTime Timestamp)> left,
        IReadOnlyList<(double Weight, DateTime Timestamp)> right)
    {
        int count = Math.Min(left.Count, right.Count);
        if (count < 2) return 0;

        double maxRFD = 0;

        for (int i = 1; i < count; i++)
        {
            double prevTotal = left[i - 1].Weight + right[i - 1].Weight;
            double currTotal = left[i].Weight + right[i].Weight;
            double dt = (left[i].Timestamp - left[i - 1].Timestamp).TotalSeconds;

            if (dt <= 0) continue;

            double rfd = (currTotal - prevTotal) / dt;
            if (rfd > maxRFD) maxRFD = rfd;
        }

        return maxRFD;
    }
}