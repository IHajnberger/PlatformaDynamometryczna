using System;
using Avalonia.Controls;

namespace UI_Test_Avalonia;

public partial class Wikipedia : UserControl
{
    public event EventHandler? BackClicked;

    public Wikipedia()
    {
        InitializeComponent();

        BackButton.Click += (sender, e) =>
        {
            BackClicked?.Invoke(this, EventArgs.Empty);
        };
    }
}