using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UI_Test_Avalonia;

public partial class MainWindow : Window
{
    private readonly UserControl _homeView;
    private readonly DataView _dataView; 
    private readonly ConfigureWifiView _configureWifiView; // Typ zmieniony z UserControl na ConfigureWifiView

    public MainWindow()
    {
        InitializeComponent();

        _homeView = new HomeView(this);
        _dataView = new DataView();
        _configureWifiView = new ConfigureWifiView();

        // Subskrypcja zdarzenia dla DataView
        _dataView.BackClicked += WifiOrDataView_BackClicked;
        
        // NOWOŚĆ: Subskrypcja zdarzenia dla ConfigureWifiView
        _configureWifiView.BackClicked += WifiOrDataView_BackClicked;
        
        MainContentArea.Content = _homeView;
    }

    // Wspólna metoda obsługująca powrót z dowolnej podstrony
    private void WifiOrDataView_BackClicked(object? sender, EventArgs e)
    {
        MainContentArea.Content = _homeView;
    }

    public void HomeButton_Click(object? sender, RoutedEventArgs e)
    {
        MainContentArea.Content = _homeView;
    }

    public void LiveDataButton_Click(object? sender, RoutedEventArgs e)
    {
        MainContentArea.Content = _dataView;
    }

    public void ConfigureWifiButton_Click(object? sender, RoutedEventArgs e)
    {
        MainContentArea.Content = _configureWifiView;
    }

    public void OnPatientsTile_Click(object? sender, RoutedEventArgs e)
    {
        // Miejsce na widok pacjentów
    }
}