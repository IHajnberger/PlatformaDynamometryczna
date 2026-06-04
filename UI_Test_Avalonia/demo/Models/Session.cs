using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UI_Test_Avalonia;

public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public string ExerciseName { get; set; } = "Przysiad (SQ)";
    public DateTime Date { get; set; } = DateTime.Now;

    // Snapshot wyników biomechanicznych
    public double PeakForceLeft { get; set; }
    public double PeakForceRight { get; set; }
    public double PeakForceTotal { get; set; }
    public double MeanForceLeft { get; set; }
    public double MeanForceRight { get; set; }
    public double AsymmetryIndex { get; set; }
    public double LoadRatioLeft { get; set; }
    public double LoadRatioRight { get; set; }
    public double MinForceLeft { get; set; }
    public double MinForceRight { get; set; }
    public double RFD { get; set; }
}

public sealed class SessionService
{
    private static readonly Lazy<SessionService> lazy = new(() => new SessionService());
    public static SessionService Instance => lazy.Value;

    private readonly string _filePath;
    private List<Session> _sessions = new();

    public IReadOnlyList<Session> Sessions => _sessions;
    public event EventHandler? SessionsChanged;

    private SessionService()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "ForcePlatformApp");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "sessions.json");
        }
        catch
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sessions.json");
        }
        Load();
    }

    public void AddSession(Session session)
    {
        _sessions.Add(session);
        Save();
        SessionsChanged?.Invoke(this, EventArgs.Empty);
    }

    // Zwraca List<Session> żeby można było sortować
    public List<Session> GetForPatient(Guid patientId)
    {
        return _sessions.FindAll(s => s.PatientId == patientId);
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
                _sessions = JsonSerializer.Deserialize<List<Session>>(File.ReadAllText(_filePath)) ?? new();
        }
        catch { _sessions = new(); }
    }

    public void Save()
    {
        try { File.WriteAllText(_filePath, JsonSerializer.Serialize(_sessions)); }
        catch { }
    }
}