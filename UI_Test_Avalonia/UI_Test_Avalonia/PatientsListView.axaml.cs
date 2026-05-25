using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UI_Test_Avalonia;

public partial class PatientsListView : UserControl
{
    public event EventHandler? BackClicked;

    public PatientsListView()
    {
        InitializeComponent();

        BackButton.Click += (sender, e) =>
        {
            BackClicked?.Invoke(this, EventArgs.Empty);
        };
    }
}