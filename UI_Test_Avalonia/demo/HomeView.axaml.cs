using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace UI_Test_Avalonia;

public partial class HomeView : UserControl
{
    public event EventHandler? TestClicked;
    public event EventHandler? PatientsClicked;
    public event EventHandler? WifiClicked;
    public event EventHandler? WikiClicked;
    public event EventHandler? AboutClicked;
    public event EventHandler? ProfileClicked;
    public event EventHandler? LogoutClicked;

    private readonly string _role;

    public HomeView(string role = "Physiotherapist")
    {
        InitializeComponent();
        _role = role;

        TileTest.Click += (s, e) => TestClicked?.Invoke(this, EventArgs.Empty);
        TilePatients.Click += (s, e) => PatientsClicked?.Invoke(this, EventArgs.Empty);
        TileWifi.Click += (s, e) => WifiClicked?.Invoke(this, EventArgs.Empty);
        TileWiki.Click += (s, e) => WikiClicked?.Invoke(this, EventArgs.Empty);
        TileAbout.Click += (s, e) => AboutClicked?.Invoke(this, EventArgs.Empty);
        TileProfile.Click += (s, e) => ProfileClicked?.Invoke(this, EventArgs.Empty);
        TileLogout.Click += (s, e) => LogoutClicked?.Invoke(this, EventArgs.Empty);

        // Subskrybuj event dopiero po InitializeComponent
        PatientService.Instance.ActivePatientChanged += OnActivePatientChanged;

        // Pierwsze odświeżenie
        UpdateActivePatientLabel();

        ApplyRoleRestrictions();
    }

    private void OnActivePatientChanged(object? sender, EventArgs e)
    {
        // Zawsze aktualizuj UI na wątku UI
        Dispatcher.UIThread.Post(UpdateActivePatientLabel);
    }

    private void ApplyRoleRestrictions()
    {
        if (_role == "Patient")
        {
            TileTest.IsVisible = false;
            TileWifi.IsVisible = false;
            PatientSelectionPanel.IsVisible = false;

        }
    }

    private void UpdateActivePatientLabel()
    {
        var p = PatientService.Instance.ActivePatient;
        ActivePatientLabel.Text = p != null ? p.FullName : "Nie wybrano pacjenta";
        ClearPatientButton.IsVisible = p != null;
    }

    private void PatientSearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var query = PatientSearchBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(query))
        {
            PatientPopup.IsOpen = false;
            return;
        }

        var results = PatientService.Instance.Patients
            .Where(p => p.FullName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        PatientListBox.ItemsSource = results;
        PatientPopup.IsOpen = results.Count > 0;
    }

    private void PatientListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PatientListBox.SelectedItem is Patient patient)
        {
            PatientService.Instance.SetActivePatient(patient);
            PatientSearchBox.Text = "";
            PatientPopup.IsOpen = false;
            PatientListBox.SelectedItem = null;
        }
    }

    private void ClearPatientButton_Click(object? sender, RoutedEventArgs e)
    {
        PatientService.Instance.SetActivePatient(null);
    }
}