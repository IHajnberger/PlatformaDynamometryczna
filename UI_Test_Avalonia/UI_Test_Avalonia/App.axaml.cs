using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System;
using System.IO;

namespace UI_Test_Avalonia;

public partial class App : Application
{
    // Ścieżka do pliku z zapisanym motywem (AppData/Local/UI_Test_Avalonia/theme.txt)
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
        "UI_Test_Avalonia", "theme.txt");

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Wczytujemy motyw przy starcie aplikacji
        LoadTheme();
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Start the MQTT Service as soon as the application framework is ready
            await MqttService.Instance.StartAsync();
            
            desktop.MainWindow = new MainWindow();

            // Ensure the MQTT service is stopped gracefully on exit
            desktop.ShutdownRequested += async (sender, e) => 
            {
                await MqttService.Instance.StopAsync();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    // --- NOWE METODY DO ZARZĄDZANIA MOTYWEM ---

    private void LoadTheme()
    {
        if (File.Exists(SettingsFilePath))
        {
            var savedTheme = File.ReadAllText(SettingsFilePath);
            if (savedTheme == "Light")
                RequestedThemeVariant = ThemeVariant.Light;
            else
                RequestedThemeVariant = ThemeVariant.Dark;
        }
    }

    public static void SetAndSaveTheme(ThemeVariant newTheme)
    {
        // 1. Zmień motyw w działającej aplikacji
        if (Current != null)
            Current.RequestedThemeVariant = newTheme;

        // 2. Zapisz wybrany motyw do pliku
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            string themeStr = newTheme == ThemeVariant.Light ? "Light" : "Dark";
            File.WriteAllText(SettingsFilePath, themeStr);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Nie udało się zapisać motywu: {ex.Message}");
        }
    }
}