using System;
using Avalonia.Controls;

namespace UI_Test_Avalonia;

public partial class MainWindow : Window
{
    // Zmienna pomocnicza, jeśli będziesz potrzebować roli w innych częściach aplikacji
    private string _currentUserRole = "Guest"; 

    public MainWindow()
    {
        InitializeComponent();
        ShowLoginScreen();
    }

    private void ShowLoginScreen()
    {
        var loginView = new LoginView();
        
        // Ekran logowania zwraca nam rolę: "Physiotherapist" lub "Patient"
        loginView.OnLoginSuccess += (role) =>
        {
            _currentUserRole = role; // Zapisujemy rolę globalnie w oknie
            ShowMainApp(role);       // Przekazujemy rolę do głównego menu
        };

        MainContentArea.Content = loginView;
    }

    // Nowa wersja metody akceptująca parametr roli
    private void ShowMainApp(string role)
    {
        // Przekazujemy rolę wprost do konstruktora HomeView
        var homeView = new HomeView(role);

        homeView.OnLiveDataClicked += () =>
        {
            var dataView = new DataView();
            // Przycisk wstecz wraca do głównego menu pamiętając aktualną rolę
            dataView.BackClicked += (s, e) => ShowMainApp(_currentUserRole); 
            MainContentArea.Content = dataView;
        };

        homeView.OnConfigureWifiClicked += () =>
        {
            var configView = new ConfigureWifiView();
            // Przycisk wstecz wraca do głównego menu pamiętając aktualną rolę
            configView.BackClicked += (s, e) => ShowMainApp(_currentUserRole);
            MainContentArea.Content = configView;
        };
        homeView.OnLogoutClicked += () =>
        {
            _currentUserRole = "Guest"; // Resetujemy uprawnienia w oknie
            ShowLoginScreen();          // Podmieniamy zawartość z powrotem na ekran logowania
        };
        MainContentArea.Content = homeView;
    }
}