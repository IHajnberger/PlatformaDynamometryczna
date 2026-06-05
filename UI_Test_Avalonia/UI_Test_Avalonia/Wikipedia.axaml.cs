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

        // Zmienione kafelki i parametry pod przysiady
        TileSQ.Click += (s, e) => ExerciseSelected?.Invoke(this, "SQ");
        TileISO.Click += (s, e) => ExerciseSelected?.Invoke(this, "ISO");
    }
}