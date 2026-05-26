using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace UI_Test_Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
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
}