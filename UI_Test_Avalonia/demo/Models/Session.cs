using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UI_Test_Avalonia;

public class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public string ExerciseName { get; set; } = "Skok pionowy";
    public DateTime Date { get; set; } = DateTime.Now;
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

    public IReadOnlyList<Session> GetForPatient(Guid patientId)
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