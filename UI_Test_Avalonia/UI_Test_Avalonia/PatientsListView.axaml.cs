using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace UI_Test_Avalonia;

public partial class PatientsListView : UserControl
{
    public event EventHandler? BackClicked;
    public event EventHandler<Patient>? PatientSelected;

    // Pod fizjo - klient
    private readonly string _role;

    // Kolory avatarów dla kolejnych pacjentów
    private static readonly string[] AvatarColors =
        { "#1d4ed8", "#7c3aed", "#b45309", "#065f46", "#be185d", "#0e7490" };

    public PatientsListView(string role = "Physiotherapist")
    {
        InitializeComponent();
        _role = role;

        BackButton.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        AddPatientButton.Click += (s, e) =>
        {
            NewPatientName.Text = "";
            NewPatientDob.Text = "";
            NewPatientPhone.Text = "";
            NewPatientNote.Text = "";
            AddPatientOverlay.IsVisible = true;
        };

        CloseOverlayButton.Click += (s, e) => AddPatientOverlay.IsVisible = false;

        ConfirmAddButton.Click += (s, e) =>
        {
            var name = NewPatientName.Text?.Trim() ?? "";
            var parts = name.Split(' ', 2);

            if (!string.IsNullOrWhiteSpace(name))
            {
                var patient = new Patient
                {
                    FirstName = parts.Length > 0 ? parts[0] : "",
                    LastName = parts.Length > 1 ? parts[1] : "",
                    PhoneNumber = NewPatientPhone.Text?.Trim() ?? "",
                    Notes = NewPatientNote.Text?.Trim() ?? ""
                };

                if (DateTime.TryParse(NewPatientDob.Text, out var dob))
                    patient.BirthDate = dob;

                PatientService.Instance.AddPatient(patient);
                RefreshList();
            }

            AddPatientOverlay.IsVisible = false;
        };

        PatientService.Instance.PatientsChanged += (s, e) => RefreshList();

        RefreshList();
        ApplyRoleRestrictions();
    }

    private void ApplyRoleRestrictions()
    {
        if (_role == "Patient")
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            var subStatusText = this.FindControl<TextBlock>("SubStatusText");

            if (statusText != null)
                statusText.Text = "Moje dane";

            if (subStatusText != null)
                subStatusText.Text = "Informacje o koncie pacjenta";

            AddPatientButton.IsVisible = false;
        }
    }

    private void RefreshList()
    {
        PatientListPanel.Children.Clear();
        var patients = PatientService.Instance.Patients;

        for (int i = 0; i < patients.Count; i++)
        {
            var patient = patients[i];
            var color = AvatarColors[i % AvatarColors.Length];
            var initials = GetInitials(patient);

            var card = BuildPatientCard(patient, initials, color);
            PatientListPanel.Children.Add(card);
        }
    }

    private string GetInitials(Patient p)
    {
        var f = string.IsNullOrEmpty(p.FirstName) ? "" : p.FirstName[0].ToString();
        var l = string.IsNullOrEmpty(p.LastName) ? "" : p.LastName[0].ToString();
        return (f + l).ToUpper();
    }

    private Border BuildPatientCard(Patient patient, string initials, string avatarColor)
    {
        var avatar = new Border
        {
            Width = 42,
            Height = 42,
            CornerRadius = new Avalonia.CornerRadius(21),
            Background = SolidColorBrush.Parse(avatarColor),
            Margin = new Avalonia.Thickness(0, 0, 14, 0),
            Child = new TextBlock
            {
                Text = initials,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        var phone = string.IsNullOrWhiteSpace(patient.PhoneNumber) ? "Brak telefonu" : patient.PhoneNumber;
        var dob = patient.BirthDate == default ? "Brak daty" : patient.BirthDate.ToString("dd.MM.yyyy");

        var infoPanel = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        infoPanel.Children.Add(new TextBlock
        {
            Text = patient.FullName,
            FontSize = 15,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White
        });

        var subLine = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        subLine.Children.Add(new TextBlock { Text = $"ur. {dob}", FontSize = 12, Foreground = Brushes.Gray });
        subLine.Children.Add(new TextBlock { Text = phone, FontSize = 12, Foreground = Brushes.Gray });
        infoPanel.Children.Add(subLine);

        var chevron = new FluentAvalonia.UI.Controls.SymbolIcon
        {
            Symbol = FluentAvalonia.UI.Controls.Symbol.ChevronRight,
            FontSize = 18,
            Foreground = Brushes.Gray,
            VerticalAlignment = VerticalAlignment.Center
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        Grid.SetColumn(avatar, 0);
        Grid.SetColumn(infoPanel, 1);
        Grid.SetColumn(chevron, 2);

        grid.Children.Add(avatar);
        grid.Children.Add(infoPanel);
        grid.Children.Add(chevron);

        var cardButton = new Button
        {
            Classes = { "DashboardCard" },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = grid
        };
        cardButton.Click += (s, e) => PatientSelected?.Invoke(this, patient);

        return new Border { Child = cardButton };
    }
}