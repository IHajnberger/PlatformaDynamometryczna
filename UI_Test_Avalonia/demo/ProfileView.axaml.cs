using System;
using Avalonia.Controls;

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

        if (mode == "physio")
        {
            PatientNameLabel.Text = "Mój profil";
            StatusText.Text = "Ustawienia konta fizjoterapeuty";
            FullNameLabel.Text = "Fizjoterapeuta (placeholder)";
            BirthDateLabel.Text = "—";
            IdLabel.Text = "FIZJO-01";
            AvatarInitials.Text = "FZ";

            
            EditNotesButton.IsVisible = true;
            NotesViewText.Text = "";
            return;
        }

        // Tryb pacjenta 
        if (patient != null)
        {

            DeletePatientButton.IsVisible = true;
            DeletePatientButton.Click += (s, e) =>
            {
                PatientService.Instance.RemovePatient(patient);
                PatientDeleted?.Invoke(this, EventArgs.Empty);
            };

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

            if (!string.IsNullOrWhiteSpace(patient.Notes))
            {
                NotesViewText.Text = patient.Notes;
                NotesViewText.Foreground = Avalonia.Media.Brushes.White;
            }
            LoadSessions(patient);
        }

        EditNotesButton.Click += (s, e) => EnterEditMode();
        CancelEditButton.Click += (s, e) => ExitEditMode(save: false);
        SaveNotesButton.Click += (s, e) => ExitEditMode(save: true);
    }

    private void LoadSessions(Patient patient)
    {
        var sessions = SessionService.Instance.GetForPatient(patient.Id);
        SessionHistoryPanel.Children.Clear();

        if (sessions.Count == 0)
        {
            SessionHistoryPanel.Children.Add(new Border
            {
                Background = Avalonia.Media.SolidColorBrush.Parse("#1c2333"), 
             });
            return;
        }

        foreach (var session in sessions)
        {
            var row = new Border
            {
                Background = Avalonia.Media.SolidColorBrush.Parse("#1a1a1a"),
                BorderBrush = Avalonia.Media.SolidColorBrush.Parse("#3d3d3d"),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(14, 10),
                Margin = new Avalonia.Thickness(0, 0, 0, 6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var nameBlock = new TextBlock
            {
                Text = session.ExerciseName,
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 13,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var dateBlock = new TextBlock
            {
                Text = session.Date.ToString("dd.MM.yyyy HH:mm"),
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 12,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            Grid.SetColumn(nameBlock, 0);
            Grid.SetColumn(dateBlock, 1);
            grid.Children.Add(nameBlock);
            grid.Children.Add(dateBlock);

            row.Child = grid;
            SessionHistoryPanel.Children.Add(row);
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
                ? Avalonia.Media.Brushes.Gray
                : Avalonia.Media.Brushes.White;
        }

        NotesViewMode.IsVisible = true;
        NotesEditBox.IsVisible = false;
        EditNotesButton.IsVisible = true;
        SaveNotesButton.IsVisible = false;
        CancelEditButton.IsVisible = false;

    }
}