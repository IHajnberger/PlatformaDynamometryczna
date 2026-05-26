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
    public double PeakForceLeft { get; set; }
    public double PeakForceRight { get; set; }
    public double PeakForceTotal { get; set; }

    public double MeanForceLeft { get; set; }
    public double MeanForceRight { get; set; }
    public double MeanForceTotal { get; set; }

    public double LoadRatioLeft { get; set; }   // %
    public double LoadRatioRight { get; set; }  // %

    public double AsymmetryIndex { get; set; }  // % (+ = lewa dominuje, - = prawa)

    public double FlightTime { get; set; }      // sekundy
    public double BrakingRFD { get; set; }      // kg/s

    public double MinForceLeft { get; set; }
    public double MinForceRight { get; set; }
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
            result.FlightTime = CalculateFlightTime(leftSamples, rightSamples);
            result.BrakingRFD = CalculateBrakingRFD(leftSamples, rightSamples);
        }
        else
        {
            // Jeśli działa tylko jedna platforma, wskaźniki dynamiczne (czas lotu, RFD) tracą sens biomechaniczny
            result.FlightTime = 0.0;
            result.BrakingRFD = 0.0;
        }

        return result;
    }

    private static double CalculateFlightTime(
        IReadOnlyList<(double Weight, DateTime Timestamp)> left,
        IReadOnlyList<(double Weight, DateTime Timestamp)> right)
    {
        int count = Math.Min(left.Count, right.Count);
        DateTime? flightStart = null;
        double maxFlight = 0;

        for (int i = 0; i < count; i++)
        {
            double total = left[i].Weight + right[i].Weight;

            if (total < FlightThreshold)
            {
                flightStart ??= left[i].Timestamp;
            }
            else if (flightStart.HasValue)
            {
                double duration = (left[i].Timestamp - flightStart.Value).TotalSeconds;
                if (duration > maxFlight) maxFlight = duration;
                flightStart = null;
            }
        }

        return maxFlight;
    }

    private static double CalculateBrakingRFD(
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