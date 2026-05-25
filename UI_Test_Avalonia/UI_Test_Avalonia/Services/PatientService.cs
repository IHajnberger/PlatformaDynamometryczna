using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UI_Test_Avalonia;

public class Patient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName => $"{FirstName} {LastName}";
    public DateTime BirthDate { get; set; }
    public string Notes { get; set; } = "";
}

public sealed class PatientService
{
    private static readonly Lazy<PatientService> lazy = new(() => new PatientService());
    public static PatientService Instance => lazy.Value;

    private readonly string _filePath;
    private List<Patient> _patients = new();

    public IReadOnlyList<Patient> Patients => _patients;
    public Patient? ActivePatient { get; private set; }

    public event EventHandler? PatientsChanged;
    public event EventHandler? ActivePatientChanged;

    private PatientService()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "ForcePlatformApp");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "patients.json");
        }
        catch
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "patients.json");
        }

        Load();

        if (_patients.Count == 0)
        {
            _patients.Add(new Patient { FirstName = "Anna", LastName = "Kowalska", BirthDate = new DateTime(1990, 3, 15) });
            _patients.Add(new Patient { FirstName = "Jan", LastName = "Nowak", BirthDate = new DateTime(1985, 7, 22) });
            _patients.Add(new Patient { FirstName = "Maria", LastName = "Wiśniewska", BirthDate = new DateTime(2001, 11, 8) });
            Save();
        }
    }

    public void SetActivePatient(Patient? patient)
    {
        ActivePatient = patient;
        ActivePatientChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddPatient(Patient patient)
    {
        _patients.Add(patient);
        Save();
        PatientsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemovePatient(Patient patient)
    {
        _patients.Remove(patient);
        if (ActivePatient?.Id == patient.Id) SetActivePatient(null);
        Save();
        PatientsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
                _patients = JsonSerializer.Deserialize<List<Patient>>(File.ReadAllText(_filePath)) ?? new();
        }
        catch
        {
            _patients = new();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(_patients));
        }
        catch { }
    }
}