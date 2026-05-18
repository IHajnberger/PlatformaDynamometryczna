using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UI_Test_Avalonia;

public partial class MainWindow : Window
{
    private readonly UserControl _configureWifiView;

    private readonly UserControl _dataView;

    // Store instances of our views
    private readonly UserControl _homeView;

    public MainWindow()
    {
        InitializeComponent();

        _homeView = new HomeView();
        _dataView = new DataView();
        _configureWifiView = new ConfigureWifiView();
        
        // Set the initial content
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
}