using System;
using Avalonia.Controls;

namespace UI_Test_Avalonia;

public partial class MainWindow : Window
{
    private string _currentUserRole = "Guest";

    public MainWindow()
    {
        InitializeComponent();
        ShowLoginScreen();
    }

    private void ShowLoginScreen()
    {
        var loginView = new LoginView();
        loginView.OnLoginSuccess += (role) =>
        {
            _currentUserRole = role;
            ShowMainApp(role);
        };
        MainContentArea.Content = loginView;
    }

    private void ShowMainApp(string role)
    {
        var homeView = new HomeView(role);

        homeView.TestClicked += (s, e) =>
        {
            var dataView = new DataView();
            dataView.BackClicked += (s, e) => ShowMainApp(_currentUserRole);
            MainContentArea.Content = dataView;
        };

        homeView.WifiClicked += (s, e) =>
        {
            var configView = new ConfigureWifiView();
            configView.BackClicked += (s, e) => ShowMainApp(_currentUserRole);
            MainContentArea.Content = configView;
        };

        homeView.PatientsClicked += (s, e) =>
        {
            var patientsView = new PatientsListView(_currentUserRole);
            patientsView.BackClicked += (s, e) => ShowMainApp(_currentUserRole);
            patientsView.PatientSelected += (s, patient) =>
            {
                var profileView = new ProfileView(patient, mode: "patient");
                profileView.BackClicked += (s, e) => MainContentArea.Content = patientsView;
                profileView.PatientDeleted += (s, e) => MainContentArea.Content = patientsView;
                profileView.SessionSelected += (s, session) =>
                {
                    var sessionDetailView = new SessionDetailView(session);
                    sessionDetailView.BackClicked += (s, e) => MainContentArea.Content = profileView;
                    MainContentArea.Content = sessionDetailView;
                };
                MainContentArea.Content = profileView;
            };
            MainContentArea.Content = patientsView;
        };

        homeView.WikiClicked += (s, e) =>
        {
            var wikiView = new Wikipedia();
            wikiView.BackClicked += (s, e) => ShowMainApp(_currentUserRole);
            wikiView.ExerciseSelected += (s, exerciseId) =>
            {
                var detailView = new ExerciseDetailView(exerciseId);
                detailView.BackClicked += (s, e) => MainContentArea.Content = wikiView;
                MainContentArea.Content = detailView;
            };
            MainContentArea.Content = wikiView;
        };

        homeView.AboutClicked += (s, e) =>
        {
            var aboutView = new About();
            aboutView.BackClicked += (s, e) => ShowMainApp(_currentUserRole);
            MainContentArea.Content = aboutView;
        };

        homeView.ProfileClicked += (s, e) =>
        {
            var profileView = _currentUserRole == "Patient"
                ? new ProfileView(PatientService.Instance.ActivePatient, mode: "patient_self")
                : new ProfileView(mode: "physio");
            profileView.BackClicked += (s, e) => ShowMainApp(_currentUserRole);
            profileView.SessionSelected += (s, session) =>
            {
                var sessionDetailView = new SessionDetailView(session);
                sessionDetailView.BackClicked += (s, e) => MainContentArea.Content = profileView;
                MainContentArea.Content = sessionDetailView;
            };
            MainContentArea.Content = profileView;
        };

        homeView.LogoutClicked += (s, e) =>
        {
            _currentUserRole = "Guest";
            PatientService.Instance.SetActivePatient(null);
            ShowLoginScreen();
        };

        MainContentArea.Content = homeView;
    }
}