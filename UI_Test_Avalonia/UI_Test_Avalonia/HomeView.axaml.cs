using Avalonia.Controls;

namespace UI_Test_Avalonia;

public partial class HomeView : UserControl
{
    // Konstruktor bezparametrowy dla designera Avalonii (wymagany!)
    public HomeView()
    {
        InitializeComponent();
    }

    // Konstruktor roboczy, do którego przekażemy okno główne
    public HomeView(MainWindow mainWindow) : this()
    {
        // Podpinamy zdarzenia kliknięcia kafli pod metody w MainWindow
        TileTest.Click += mainWindow.LiveDataButton_Click;
        //TilePatients.Click += mainWindow.OnPatientsTile_Click; // Musimy dodać tę metodę w MainWindow
        TileWifi.Click += mainWindow.ConfigureWifiButton_Click;
        TileAbout.Click += (s, e) => { /* Tutaj możesz obsłużyć About */ };
    }
}