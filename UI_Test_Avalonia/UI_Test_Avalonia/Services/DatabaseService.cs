using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;

namespace UI_Test_Avalonia.Services;

public class PatientSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string ExerciseName { get; set; } = "Test platformy dynamometrycznej";

    // Dane statystyczne sesji
    public double PeakForceLeft { get; set; }
    public double PeakForceRight { get; set; }
    public double MeanForceLeft { get; set; }
    public double MeanForceRight { get; set; }
    public double AsymmetryIndex { get; set; }

    // Zapisać możemy również spłaszczone ciągi próbek z wykresu, aby odtworzyć go w historii
    public string RawLeftValuesJson { get; set; } = "[]";
    public string RawRightValuesJson { get; set; } = "[]";
}

public sealed class DatabaseService
{
    private static readonly Lazy<DatabaseService> _lazy = new(() => new DatabaseService());
    public static DatabaseService Instance => _lazy.Value;

    private readonly string _connectionString;

    private DatabaseService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "ForcePlatformApp");
        Directory.CreateDirectory(dir);
        var dbPath = Path.Combine(dir, "application.db");

        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Tabela Pacjentów
        var cmdPatients = connection.CreateCommand();
        cmdPatients.CommandText = @"
            CREATE TABLE IF NOT EXISTS Patients (
                Id TEXT PRIMARY KEY,
                FirstName TEXT,
                LastName TEXT,
                BirthDate TEXT,
                Notes TEXT,
                PhoneNumber TEXT
            );";
        cmdPatients.ExecuteNonQuery();

        // Tabela Sesji
        var cmdSessions = connection.CreateCommand();
        cmdSessions.CommandText = @"
            CREATE TABLE IF NOT EXISTS Sessions (
                Id TEXT PRIMARY KEY,
                PatientId TEXT,
                Date TEXT,
                ExerciseName TEXT,
                PeakForceLeft REAL,
                PeakForceRight REAL,
                MeanForceLeft REAL,
                MeanForceRight REAL,
                AsymmetryIndex REAL,
                RawLeftValuesJson TEXT,
                RawRightValuesJson TEXT,
                FOREIGN KEY(PatientId) REFERENCES Patients(Id) ON DELETE CASCADE
            );";
        cmdSessions.ExecuteNonQuery();
    }

    public SqliteConnection GetConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
}