using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace UI_Test_Avalonia.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly DispatcherTimer _updateTimer;

    // Zmieniamy na double, ponieważ MqttService używa typu double w kolejkach
    private double _leftWeight;
    public double LeftWeight
    {
        get => _leftWeight;
        set
        {
            _leftWeight = value;
            OnPropertyChanged();
        }
    }

    private double _rightWeight;
    public double RightWeight
    {
        get => _rightWeight;
        set
        {
            _rightWeight = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel()
    {
        // 1. Uruchamiamy wbudowany serwer MQTT w tle, aby nie zamrażać interfejsu
        _ = StartMqttServerAsync();

        // 2. Konfigurujemy licznik (Timer), który będzie działał bezpośrednio w wątku UI.
        // Odpytuje on bezpieczne kolejki sieciowe co 20 ms i płynnie aktualizuje ekran.
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20)
        };
        _updateTimer.Tick += OnUpdateTimerTick;
        _updateTimer.Start();
    }

    private async Task StartMqttServerAsync()
    {
        try
        {
            await MqttService.Instance.StartAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Błąd startu brokera MQTT: {ex.Message}");
        }
    }

    private void OnUpdateTimerTick(object? sender, EventArgs e)
    {
        bool hasNewData = false;
        double currentLeft = LeftWeight;
        double currentRight = RightWeight;

        // Konsumujemy nagromadzone próbki z wątku sieciowego, zachowując tylko najnowszą
        while (MqttService.Instance.Device1Queue.TryDequeue(out var leftSample))
        {
            currentLeft = leftSample.Weight;
            hasNewData = true;
        }

        while (MqttService.Instance.Device2Queue.TryDequeue(out var rightSample))
        {
            currentRight = rightSample.Weight;
            hasNewData = true;
        }

        // Jeśli wpadła nowa paczka danych przez Wi-Fi, odświeżamy zbindowane w oknie właściwości
        if (hasNewData)
        {
            LeftWeight = currentLeft;
            RightWeight = currentRight;
        }
    }

    // Metoda sprzątająca (warto wywołać ją np. podczas zamykania głównego widoku)
    public async Task ShutdownAsync()
    {
        _updateTimer.Stop();
        await MqttService.Instance.StopAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}