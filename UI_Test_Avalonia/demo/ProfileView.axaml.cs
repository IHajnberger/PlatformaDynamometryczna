using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace UI_Test_Avalonia;

public partial class ProfileView : UserControl
{
    public event EventHandler? BackClicked;
    public event EventHandler? PatientDeleted;

    private Patient? _patient;
    private string _originalNotes = "";

    public ProfileView(Patient? patient = null, string mode = "patient")
    {
        InitializeComponent();
        _patient = patient;

        BackButton.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        switch (mode)
        {
            case "physio":
                LoadPhysioSelfProfile();
                break;
            case "patient_self":
                LoadPatientSelfProfile(patient);
                break;
            default: // "patient" – fizjo ogląda profil pacjenta
                LoadPhysioViewOfPatient(patient);
                break;
        }
    }

    // ── Fizjo – własny profil ────────────────────────────────────────────
    private void LoadPhysioSelfProfile()
    {
        PatientNameLabel.Text = "Mój profil";
        StatusText.Text = "Ustawienia konta fizjoterapeuty";
        FullNameLabel.Text = "Fizjoterapeuta (placeholder)";
        BirthDateLabel.Text = "—";
        IdLabel.Text = "FIZJO-01";
        AvatarInitials.Text = "FZ";
        // Nic więcej – bez notatek, sesji, wykresu
    }

    // ── Fizjo – ogląda profil pacjenta ──────────────────────────────────
    private void LoadPhysioViewOfPatient(Patient? patient)
    {
        if (patient == null) return;

        SetBasicData(patient);

        DeletePatientButton.IsVisible = true;
        DeletePatientButton.Click += (s, e) =>
        {
            PatientService.Instance.RemovePatient(patient);
            PatientDeleted?.Invoke(this, EventArgs.Empty);
        };

        EditDataButton.IsVisible = true;

        // Notatki – edytowalne
        NotesSectionBorder.IsVisible = true;
        if (!string.IsNullOrWhiteSpace(patient.Notes))
        {
            NotesViewText.Text = patient.Notes;
            NotesViewText.Foreground = Brushes.White;
        }
        EditNotesButton.Click += (s, e) => EnterEditMode();
        CancelEditButton.Click += (s, e) => ExitEditMode(save: false);
        SaveNotesButton.Click += (s, e) => ExitEditMode(save: true);

        // Wykres + sesje
        TrendSectionBorder.IsVisible = true;
        SessionSectionBorder.IsVisible = true;
        LoadTrendChart(patient);
        LoadSessions(patient);
    }

    // ── Pacjent – własny profil ──────────────────────────────────────────
    private void LoadPatientSelfProfile(Patient? patient)
    {
        if (patient == null)
        {
            // Brak zalogowanego pacjenta – placeholder
            PatientNameLabel.Text = "Mój profil";
            StatusText.Text = "Twoje dane i historia ćwiczeń";
            FullNameLabel.Text = "Pacjent (placeholder)";
            BirthDateLabel.Text = "—";
            IdLabel.Text = "—";
            AvatarInitials.Text = "P";
            TrendSectionBorder.IsVisible = true;
            SessionSectionBorder.IsVisible = true;
            LoadPlaceholderChart();
            return;
        }

        SetBasicData(patient);
        StatusText.Text = "Twoje dane i historia ćwiczeń";

        // Notatki – tylko do odczytu (pokazujemy jako zwykły tekst, bez przycisku edycji)
        NotesSectionBorder.IsVisible = true;
        EditNotesButton.IsVisible = false;
        NotesViewText.Text = string.IsNullOrWhiteSpace(patient.Notes)
            ? "Brak notatek od fizjoterapeuty."
            : patient.Notes;
        NotesViewText.Foreground = string.IsNullOrWhiteSpace(patient.Notes)
            ? Brushes.Gray : Brushes.White;

        // Wykres + sesje
        TrendSectionBorder.IsVisible = true;
        SessionSectionBorder.IsVisible = true;
        LoadTrendChart(patient);
        LoadSessions(patient);
    }

    // ── Helpers ─────────────────────────────────────────────────────────
    private void SetBasicData(Patient patient)
    {
        PatientNameLabel.Text = patient.FullName;
        FullNameLabel.Text = patient.FullName;
        BirthDateLabel.Text = patient.BirthDate == default
            ? "Nie podano"
            : patient.BirthDate.ToString("dd.MM.yyyy");
        IdLabel.Text = patient.Id.ToString()[..8].ToUpper();

        var parts = patient.FullName.Split(' ');
        AvatarInitials.Text = parts.Length >= 2
            ? $"{parts[0][0]}{parts[1][0]}"
            : patient.FullName[..Math.Min(2, patient.FullName.Length)].ToUpper();
    }

    private void LoadTrendChart(Patient patient)
    {
        var sessions = SessionService.Instance.GetForPatient(patient.Id);

        if (sessions.Count == 0)
        {
            LoadPlaceholderChart();
            return;
        }

        // Placeholder wartości – docelowo z modelu sesji
        var values = new ObservableCollection<double>();
        for (int i = 0; i < sessions.Count; i++)
            values.Add(25 + i * 1.5 + new Random(i).Next(-3, 4)); // fake trend rosnący

        SetupChart(values);
    }

    private void LoadPlaceholderChart()
    {
        var values = new ObservableCollection<double> { 22, 24, 23, 26, 25, 28, 27, 30 };
        SetupChart(values);
    }

    private void SetupChart(ObservableCollection<double> values)
    {
        var color = SKColor.Parse("#10b981");

        TrendChart.Series = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = values,
                GeometrySize = 8,
                LineSmoothness = 0.5,
                Stroke = new SolidColorPaint(color) { StrokeThickness = 3 },
                GeometryStroke = new SolidColorPaint(color) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColors.White),
                Fill = new LinearGradientPaint(
                    new[] { color.WithAlpha(50), color.WithAlpha(0) },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1))
            }
        };

        TrendChart.XAxes = new Axis[]
        {
            new Axis
            {
                Labels = null,
                TextSize = 0,
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1 }
            }
        };

        TrendChart.YAxes = new Axis[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#888888")),
                TextSize = 11,
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#333333")) { StrokeThickness = 1 },
                Labeler = val => $"{val:F0} cm"
            }
        };
    }

    private void LoadSessions(Patient patient)
    {
        var sessions = SessionService.Instance.GetForPatient(patient.Id);
        SessionHistoryPanel.Children.Clear();

        if (sessions.Count == 0)
        {
            SessionHistoryPanel.Children.Add(new Border
            {
                Background = SolidColorBrush.Parse("#1c2333"),
                BorderBrush = SolidColorBrush.Parse("#2d3a55"),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(14, 10),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new FluentAvalonia.UI.Controls.SymbolIcon { Symbol = FluentAvalonia.UI.Controls.Symbol.Important, FontSize = 14, Foreground = SolidColorBrush.Parse("#3b82f6") },
                        new TextBlock { Text = "Brak zapisanych sesji.", FontSize = 12, Foreground = SolidColorBrush.Parse("#60a5fa"), VerticalAlignment = VerticalAlignment.Center }
                    }
                }
            });
            return;
        }

        // Pokaż max 10 ostatnich, od najnowszej
        var toShow = new System.Collections.Generic.List<Session>(sessions);
        toShow.Sort((a, b) => b.Date.CompareTo(a.Date));
        if (toShow.Count > 10) toShow = toShow.GetRange(0, 10);

        foreach (var session in toShow)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            // Ikona ćwiczenia
            var icon = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new Avalonia.CornerRadius(8),
                Background = SolidColorBrush.Parse("#1d3a6e"),
                Margin = new Avalonia.Thickness(0, 0, 12, 0),
                Child = new FluentAvalonia.UI.Controls.SymbolIcon
                {
                    Symbol = FluentAvalonia.UI.Controls.Symbol.Up,
                    FontSize = 14,
                    Foreground = SolidColorBrush.Parse("#3b82f6"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var nameBlock = new TextBlock
            {
                Text = session.ExerciseName,
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            var dateBlock = new TextBlock
            {
                Text = session.Date.ToString("dd.MM.yyyy  HH:mm"),
                Foreground = Brushes.Gray,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 16, 0)
            };

            // Przycisk szczegółów – placeholder
            var detailBtn = new Button
            {
                Background = SolidColorBrush.Parse("#1e3a5f"),
                BorderThickness = new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(10, 6),
                VerticalAlignment = VerticalAlignment.Center,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Children =
                    {
                        new FluentAvalonia.UI.Controls.SymbolIcon { Symbol = FluentAvalonia.UI.Controls.Symbol.Open, FontSize = 12, Foreground = SolidColorBrush.Parse("#3b82f6") },
                        new TextBlock { Text = "Szczegóły", FontSize = 12, Foreground = SolidColorBrush.Parse("#3b82f6"), VerticalAlignment = VerticalAlignment.Center }
                    }
                }
            };
            detailBtn.Click += (s, e) => { /* placeholder */ };

            Grid.SetColumn(icon, 0);
            Grid.SetColumn(nameBlock, 1);
            Grid.SetColumn(dateBlock, 2);
            Grid.SetColumn(detailBtn, 3);

            grid.Children.Add(icon);
            grid.Children.Add(nameBlock);
            grid.Children.Add(dateBlock);
            grid.Children.Add(detailBtn);

            SessionHistoryPanel.Children.Add(new Border
            {
                Background = SolidColorBrush.Parse("#1a1a1a"),
                BorderBrush = SolidColorBrush.Parse("#3d3d3d"),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(10),
                Padding = new Avalonia.Thickness(14, 10),
                Margin = new Avalonia.Thickness(0, 0, 0, 6),
                Child = grid
            });
        }
    }

    private void EnterEditMode()
    {
        _originalNotes = _patient?.Notes ?? "";
        NotesEditBox.Text = _originalNotes;
        NotesViewMode.IsVisible = false;
        NotesEditBox.IsVisible = true;
        EditNotesButton.IsVisible = false;
        SaveNotesButton.IsVisible = true;
        CancelEditButton.IsVisible = true;
        NotesEditBox.Focus();
    }

    private void ExitEditMode(bool save)
    {
        if (save && _patient != null)
        {
            _patient.Notes = NotesEditBox.Text ?? "";
            PatientService.Instance.Save();
            var notes = _patient.Notes;
            NotesViewText.Text = string.IsNullOrWhiteSpace(notes)
                ? "Brak notatek. Kliknij 'Edytuj' aby dodać."
                : notes;
            NotesViewText.Foreground = string.IsNullOrWhiteSpace(notes)
                ? Brushes.Gray : Brushes.White;
        }
        NotesViewMode.IsVisible = true;
        NotesEditBox.IsVisible = false;
        EditNotesButton.IsVisible = true;
        SaveNotesButton.IsVisible = false;
        CancelEditButton.IsVisible = false;
    }
}