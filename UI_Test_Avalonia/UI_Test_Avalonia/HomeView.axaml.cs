using System;
using Avalonia.Controls;

namespace UI_Test_Avalonia;

public partial class HomeView : UserControl
{
    public event Action? OnLiveDataClicked;
    public event Action? OnConfigureWifiClicked;
    public event Action? OnLogoutClicked; // NOWOŚĆ: Zdarzenie wylogowania

    public HomeView(string role)
    {
        InitializeComponent();

        // Podpięcie dotychczasowych przycisków
        var liveDataBtn = this.FindControl<Button>("TileTest");
        if (liveDataBtn != null) liveDataBtn.Click += (s, e) => OnLiveDataClicked?.Invoke();

        var configWifiBtn = this.FindControl<Button>("TileWifi");
        if (configWifiBtn != null) configWifiBtn.Click += (s, e) => OnConfigureWifiClicked?.Invoke();

        // NOWOŚĆ: Podpięcie przycisku wylogowania
        var logoutBtn = this.FindControl<Button>("TileLogout");
        if (logoutBtn != null)
        {
            logoutBtn.Click += (s, e) => OnLogoutClicked?.Invoke();
        }

        ApplyPermissions(role);
    }

    private void ApplyPermissions(string role)
    {
        var tilePatients = this.FindControl<Button>("TilePatients");
        var tileWifi = this.FindControl<Button>("TileWifi");

        if (role == "Patient")
        {
            if (tilePatients != null) tilePatients.IsVisible = false;
            if (tileWifi != null) tileWifi.IsVisible = false;
        }
    }
}