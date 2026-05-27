using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using UI_Test_Avalonia.Services;

namespace UI_Test_Avalonia;

public sealed class PatientService
{
    private static readonly Lazy<PatientService> lazy = new(() => new PatientService());
    public static PatientService Instance => lazy.Value;


    private List<Patient> _patients = new();
    public IReadOnlyList<Patient> Patients => _patients;
    public Patient? ActivePatient { get; private set; }

    public event EventHandler? PatientsChanged;
    public event EventHandler? ActivePatientChanged;

    private PatientService()
    {
        Load();

        if (_patients.Count == 0)
        {
            AddPatient(new Patient { FirstName = "Anna", LastName = "Kowalska", BirthDate = new DateTime(1990, 3, 15) });
            AddPatient(new Patient { FirstName = "Jan", LastName = "Nowak", BirthDate = new DateTime(1985, 7, 22) });
            AddPatient(new Patient { FirstName = "Maria", LastName = "Wiśniewska", BirthDate = new DateTime(2001, 11, 8) });
        }
    }

    public void SetActivePatient(Patient? patient)
    {
        ActivePatient = patient;
        ActivePatientChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddPatient(Patient patient)
    {
        using var conn = DatabaseService.Instance.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO Patients (Id, FirstName, LastName, BirthDate, Notes, PhoneNumber) VALUES ($id, $fn, $ln, $bd, $nt, $pn)";
        cmd.Parameters.AddWithValue("$id", patient.Id.ToString());
        cmd.Parameters.AddWithValue("$fn", patient.FirstName);
        cmd.Parameters.AddWithValue("$ln", patient.LastName);
        cmd.Parameters.AddWithValue("$bd", patient.BirthDate.ToString("o"));
        cmd.Parameters.AddWithValue("$nt", patient.Notes);
        cmd.Parameters.AddWithValue("$pn", patient.PhoneNumber);
        cmd.ExecuteNonQuery();

        Load();
        PatientsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemovePatient(Patient patient)
    {
        using var conn = DatabaseService.Instance.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Patients WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", patient.Id.ToString());
        cmd.ExecuteNonQuery();

        if (ActivePatient?.Id == patient.Id) SetActivePatient(null);
        Load();
        PatientsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save() // Dla wstecznej kompatybilności np. aktualizacja notatek
    {
        if (ActivePatient == null) return;
        using var conn = DatabaseService.Instance.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Patients SET FirstName=$fn, LastName=$ln, Notes=$nt, PhoneNumber=$pn WHERE Id=$id";
        cmd.Parameters.AddWithValue("$id", ActivePatient.Id.ToString());
        cmd.Parameters.AddWithValue("$fn", ActivePatient.FirstName);
        cmd.Parameters.AddWithValue("$ln", ActivePatient.LastName);
        cmd.Parameters.AddWithValue("$nt", ActivePatient.Notes);
        cmd.Parameters.AddWithValue("$pn", ActivePatient.PhoneNumber);
        cmd.ExecuteNonQuery();
        Load();
    }

    private void Load()
    {
        _patients.Clear();
        using var conn = DatabaseService.Instance.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, FirstName, LastName, BirthDate, Notes, PhoneNumber FROM Patients";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            _patients.Add(new Patient
            {
                Id = Guid.Parse(reader.GetString(0)),
                FirstName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                LastName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                BirthDate = reader.IsDBNull(3) ? default : DateTime.Parse(reader.GetString(3)),
                Notes = reader.IsDBNull(4) ? "" : reader.GetString(4),
                PhoneNumber = reader.IsDBNull(5) ? "" : reader.GetString(5)
            });
        }
    }
}