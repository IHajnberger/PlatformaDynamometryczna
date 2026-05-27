using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using UI_Test_Avalonia.Services;

namespace UI_Test_Avalonia;

public sealed class SessionService
{
    private static readonly Lazy<SessionService> _lazy = new(() => new SessionService());
    public static SessionService Instance => _lazy.Value;

    private SessionService() { }

    public void SaveSession(PatientSession session)
    {
        using var conn = DatabaseService.Instance.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Sessions 
            (Id, PatientId, Date, ExerciseName, PeakForceLeft, PeakForceRight, MeanForceLeft, MeanForceRight, AsymmetryIndex, RawLeftValuesJson, RawRightValuesJson) 
            VALUES 
            ($id, $pId, $date, $name, $pL, $pR, $mL, $mR, $asym, $rawL, $rawR)";

        cmd.Parameters.AddWithValue("$id", session.Id.ToString());
        cmd.Parameters.AddWithValue("$pId", session.PatientId.ToString());
        cmd.Parameters.AddWithValue("$date", session.Date.ToString("o"));
        cmd.Parameters.AddWithValue("$name", session.ExerciseName);
        cmd.Parameters.AddWithValue("$pL", session.PeakForceLeft);
        cmd.Parameters.AddWithValue("$pR", session.PeakForceRight);
        cmd.Parameters.AddWithValue("$mL", session.MeanForceLeft);
        cmd.Parameters.AddWithValue("$mR", session.MeanForceRight);
        cmd.Parameters.AddWithValue("$asym", session.AsymmetryIndex);
        cmd.Parameters.AddWithValue("$rawL", session.RawLeftValuesJson);
        cmd.Parameters.AddWithValue("$rawR", session.RawRightValuesJson);

        cmd.ExecuteNonQuery();
    }

    public List<PatientSession> GetForPatient(Guid patientId)
    {
        var list = new List<PatientSession>();
        using var conn = DatabaseService.Instance.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, PatientId, Date, ExerciseName, PeakForceLeft, PeakForceRight, MeanForceLeft, MeanForceRight, AsymmetryIndex, RawLeftValuesJson, RawRightValuesJson FROM Sessions WHERE PatientId = $pId ORDER BY Date DESC";
        cmd.Parameters.AddWithValue("$pId", patientId.ToString());

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new PatientSession
            {
                Id = Guid.Parse(reader.GetString(0)),
                PatientId = Guid.Parse(reader.GetString(1)),
                Date = DateTime.Parse(reader.GetString(2)),
                ExerciseName = reader.GetString(3),
                PeakForceLeft = reader.GetDouble(4),
                PeakForceRight = reader.GetDouble(5),
                MeanForceLeft = reader.GetDouble(6),
                MeanForceRight = reader.GetDouble(7),
                AsymmetryIndex = reader.GetDouble(8),
                RawLeftValuesJson = reader.GetString(9),
                RawRightValuesJson = reader.GetString(10)
            });
        }
        return list;
    }
}