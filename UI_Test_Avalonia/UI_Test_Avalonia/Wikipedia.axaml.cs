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

        TileSRC.Click += (s, e) => ExerciseSelected?.Invoke(this, "SRC");
        TilePJ.Click += (s, e) => ExerciseSelected?.Invoke(this, "PJ");
        TileTWiS.Click += (s, e) => ExerciseSelected?.Invoke(this, "TWiS");
        TilePO.Click += (s, e) => ExerciseSelected?.Invoke(this, "PO");
        TilePR.Click += (s, e) => ExerciseSelected?.Invoke(this, "PR");
        TileTWiI.Click += (s, e) => ExerciseSelected?.Invoke(this, "TWiI");
        TileTPC.Click += (s, e) => ExerciseSelected?.Invoke(this, "TPC");
    }
}