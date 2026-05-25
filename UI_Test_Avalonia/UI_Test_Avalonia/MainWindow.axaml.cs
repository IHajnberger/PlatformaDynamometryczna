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
        var homeView = new HomeView(role); // ← przekaż rolę

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
            var patientsView = new PatientsListView();
            patientsView.BackClicked += (s, e) => ShowMainApp(_currentUserRole);
            MainContentArea.Content = patientsView;
        };

        homeView.WikiClicked += (s, e) =>
        {
            var wikiView = new Wikipedia();
            wikiView.BackClicked += (s, e) => ShowMainApp(_currentUserRole);
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
            var profileView = new ProfileView();
            profileView.BackClicked += (s, e) => ShowMainApp(_currentUserRole);
            MainContentArea.Content = profileView;
        };

        homeView.LogoutClicked += (s, e) =>
        {
            _currentUserRole = "Guest";
            ShowLoginScreen();
        };

        MainContentArea.Content = homeView;
    }
}