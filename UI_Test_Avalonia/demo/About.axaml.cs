using System;
using Avalonia.Controls;

namespace UI_Test_Avalonia;

public partial class About : UserControl
{
    public event EventHandler? BackClicked;

    public About()
    {
        InitializeComponent();
        
        BackButton.Click += (sender, e) =>
        {
            BackClicked?.Invoke(this, EventArgs.Empty);
        };
    }
}