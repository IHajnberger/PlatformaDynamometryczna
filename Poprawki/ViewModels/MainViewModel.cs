using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using UI_Test_Avalonia.Models;
using UI_Test_Avalonia.Services;

namespace UI_Test_Avalonia.ViewModels;


//tu też są serial wrzucone, do zamiany na mqtt

public class MainViewModel : INotifyPropertyChanged
{
    private readonly SerialService _serialService;

    private float _leftWeight;
    public float LeftWeight
    {
        get => _leftWeight;
        set
        {
            _leftWeight = value;
            OnPropertyChanged();
        }
    }

    private float _rightWeight;
    public float RightWeight
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
        _serialService = new SerialService();

        _serialService.DataReceived += OnDataReceived;

        _serialService.Connect("COM5");
    }

    private void OnDataReceived(PlatformData data)
    {
        Dispatcher.UIThread.Post(() =>
        {
            LeftWeight = data.LeftWeight;
            RightWeight = data.RightWeight;
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}