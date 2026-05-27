using System;
using Avalonia.Controls;

namespace UI_Test_Avalonia;

public partial class Wikipedia : UserControl
{
    public event EventHandler? BackClicked;
    public event EventHandler<string>? ExerciseSelected;

    public Wikipedia()
    {
        InitializeComponent();

        BackButton.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        TileCMJ.Click += (s, e) => ExerciseSelected?.Invoke(this, "CMJ");
        TileSQJ.Click += (s, e) => ExerciseSelected?.Invoke(this, "SQJ");
    }
}