using System;
using System.Linq;
using Avalonia.Controls;

namespace UI_Test_Avalonia;

public partial class LoginView : UserControl
{
    public event Action<string>? OnLoginSuccess;

    public LoginView()
    {
        InitializeComponent();

        PhysioLoginButton.Click += (s, e) => OnLoginSuccess?.Invoke("Physiotherapist");
        PatientLoginButton.Click += (s, e) => OnLoginSuccess?.Invoke("Patient");
    }
}