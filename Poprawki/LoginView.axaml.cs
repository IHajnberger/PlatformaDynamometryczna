using System;
using Avalonia.Controls;

namespace UI_Test_Avalonia;

public partial class LoginView : UserControl
{
    public event Action<string>? OnLoginSuccess;

    public LoginView()
    {
        InitializeComponent();

        // Reakcja na kliknięcie przycisku Fizjoterapeuty
        PhysioLoginButton.Click += (s, e) =>
        {
            OnLoginSuccess?.Invoke("Physiotherapist");
        };

        // Reakcja na kliknięcie przycisku Pacjenta
        PatientLoginButton.Click += (s, e) =>
        {
            OnLoginSuccess?.Invoke("Patient");
        };
    }
}