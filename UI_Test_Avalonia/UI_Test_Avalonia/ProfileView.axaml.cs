using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UI_Test_Avalonia;

public partial class ProfileView : UserControl
{
    public event EventHandler? BackClicked;
    public event EventHandler? PatientDeleted;
    public event EventHandler<Session>? SessionSelected;

    private Patient? _patient;
    private string _originalNotes = "";

    public ProfileView(Patient? patient = null, string mode = "patient")
    {
        InitializeComponent();
        _patient = patient;

        BackButton.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        switch (mode)
        {
            case "physio":
                LoadPhysioSelfProfile();
                break;
            case "patient_self":
                LoadPatientSelfProfile(patient);
                break;
            default:
                LoadPhysioViewOfPatient(patient);
                break;
        }
    }

    private void LoadPhysioSelfProfile()
    {
        PatientNameLabel.Text = "Mój profil";
        StatusText.Text = "Ustawienia konta fizjoterapeuty";
        FullNameLabel.Text = "Fizjoterapeuta (placeholder)";
        BirthDateLabel.Text = "—";
        IdLabel.Text = "FIZJO-01";
        AvatarInitials.Text = "FZ";
    }

    private void LoadPhysioViewOfPatient(Patient? patient)
    {
        if (patient == null) return;

        SetBasicData(patient);

        DeletePatientButton.IsVisible = true;
        DeletePatientButton.Click += (s, e) =>
        {
            PatientService.Instance.RemovePatient(patient);
            PatientDeleted?.Invoke(this, EventArgs.Empty);
        };

        EditDataButton.IsVisible = true;

        NotesSectionBorder.IsVisible = true;
        if (!string.IsNullOrWhiteSpace(patient.Notes))
        {
            NotesViewText.Text = patient.Notes;
            NotesViewText.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextPrimaryBrush"));
        }
        EditNotesButton.Click += (s, e) => EnterEditMode();
        CancelEditButton.Click += (s, e) => ExitEditMode(save: false);
        SaveNotesButton.Click += (s, e) => ExitEditMode(save: true);

        TrendSectionBorder.IsVisible = true;
        SessionSectionBorder.IsVisible = true;
        LoadTrendChart(patient);
        LoadSessions(patient);
    }

    private void LoadPatientSelfProfile(Patient? patient)
    {
        if (patient == null)
        {
            PatientNameLabel.Text = "Mój profil";
            StatusText.Text = "Twoje dane i historia ćwiczeń";
            FullNameLabel.Text = "Pacjent (placeholder)";
            BirthDateLabel.Text = "—";
            IdLabel.Text = "—";
            AvatarInitials.Text = "P";
            TrendSectionBorder.IsVisible = true;
            SessionSectionBorder.IsVisible = true;
            LoadPlaceholderChart();
            return;
        }

        SetBasicData(patient);
        StatusText.Text = "Twoje dane i historia ćwiczeń";

        NotesSectionBorder.IsVisible = true;
        EditNotesButton.IsVisible = false;
        NotesViewText.Text = string.IsNullOrWhiteSpace(patient.Notes)
            ? "Brak notatek od fizjoterapeuty."
            : patient.Notes;

        string brushName = string.IsNullOrWhiteSpace(patient.Notes) ? "TextSecondaryBrush" : "TextPrimaryBrush";
        NotesViewText.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable(brushName));

        TrendSectionBorder.IsVisible = true;
        SessionSectionBorder.IsVisible = true;
        LoadTrendChart(patient);
        LoadSessions(patient);
    }

    private void SetBasicData(Patient patient)
    {
        PatientNameLabel.Text = patient.FullName;
        FullNameLabel.Text = patient.FullName;
        
        BirthDateLabel.Text = patient.BirthDate == default
            ? "Nie podano"
            : patient.BirthDate.ToString("dd.MM.yyyy");

        // Bezpieczne parsowanie ID
        string idStr = patient.Id.ToString();
        IdLabel.Text = idStr.Length >= 8 ? idStr[..8].ToUpper() : idStr.ToUpper();

        // Bezpieczne pobieranie inicjałów bez błędu IndexOutOfRange
        string f = string.IsNullOrWhiteSpace(patient.FirstName) ? "" : patient.FirstName[0].ToString();
        string l = string.IsNullOrWhiteSpace(patient.LastName) ? "" : patient.LastName[0].ToString();
        string initials = (f + l).ToUpper();

        if (string.IsNullOrWhiteSpace(initials))
        {
            initials = "?";
        }

        AvatarInitials.Text = initials;
    }

    private void LoadTrendChart(Patient patient)
    {
        var sessions = SessionService.Instance.GetForPatient(patient.Id);

        if (sessions.Count == 0)
        {
            LoadPlaceholderChart();
            return;
        }

        var sorted = new List<Session>(sessions);
        sorted.Sort((a, b) => a.Date.CompareTo(b.Date));

        var data = sorted.Select(s => (
            Value: s.AsymmetryIndex,
            Label: s.Date.ToString("dd.MM")
        )).ToList();
        
        RenderChart(data);
    }

    private void LoadPlaceholderChart()
    {
        var data = new List<(double Value, string Label)>
        {
            (14.0, "01.05"), (10.0, "04.05"), (-8.0, "08.05"),
            (6.0, "12.05"), (-4.0, "15.05"), (3.0, "19.05"),
            (-2.0, "22.05"), (1.0, "26.05")
        };
        RenderChart(data);
    }

    private void RenderChart(List<(double Value, string Label)> data)
    {
        const int W = 900;
        const int H = 280;
        const int padL = 52;
        const int padR = 20;
        const int padT = 30;
        const int padB = 36;

        int chartW = W - padL - padR;
        int chartH = H - padT - padB;

        double maxAbs = data.Count > 0 ? data.Max(d => Math.Abs(d.Value)) : 10;
        maxAbs = Math.Max(maxAbs + 4, 14);

        double zeroY = padT + chartH / 2.0;
        double step = (double)chartW / Math.Max(data.Count, 1);
        double barW = Math.Min(44, step * 0.52);

        using var bitmap = new SKBitmap(W, H);
        using var canvas = new SKCanvas(bitmap);
        
        // Zawsze przezroczyste tło, by odziedziczyć kolor po kontrolce z Avalonia
        canvas.Clear(SKColors.Transparent);

        // Sprawdzamy aktualny motyw z Avalonia UI
        bool isDarkTheme = Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark;

        var gridColor = isDarkTheme ? SKColor.Parse("#2d2d2d") : SKColor.Parse("#e5e7eb");
        var textColor = isDarkTheme ? SKColor.Parse("#888888") : SKColor.Parse("#6b7280");
        var zeroLineColor = isDarkTheme ? SKColor.Parse("#555555") : SKColor.Parse("#9ca3af");

        using var chartBgPaint = new SKPaint { Color = SKColors.Transparent, IsAntialias = true };
        var chartRect = new SKRoundRect(new SKRect(padL, padT, W - padR, H - padB), 6);
        canvas.DrawRoundRect(chartRect, chartBgPaint);

        using var gridPaint = new SKPaint
        {
            Color = gridColor,
            StrokeWidth = 1,
            IsAntialias = false
        };

        using var axisTextPaint = new SKPaint
        {
            Color = textColor,
            TextSize = 12,
            IsAntialias = true,
            TextAlign = SKTextAlign.Right
        };

        foreach (var pct in new[] { 5, 10, 15 })
        {
            double posY = zeroY - (chartH / 2.0) * pct / maxAbs;
            double negY = zeroY + (chartH / 2.0) * pct / maxAbs;

            if (posY >= padT)
            {
                canvas.DrawLine(padL, (float)posY, W - padR, (float)posY, gridPaint);
                canvas.DrawText($"+{pct}%", padL - 5, (float)posY + 4, axisTextPaint);
            }
            if (negY <= H - padB)
            {
                canvas.DrawLine(padL, (float)negY, W - padR, (float)negY, gridPaint);
                canvas.DrawText($"-{pct}%", padL - 5, (float)negY + 4, axisTextPaint);
            }
        }

        using var zeroPaint = new SKPaint
        {
            Color = zeroLineColor,
            StrokeWidth = 1.5f,
            IsAntialias = false
        };
        canvas.DrawLine(padL, (float)zeroY, W - padR, (float)zeroY, zeroPaint);
        canvas.DrawText("0%", padL - 5, (float)zeroY + 4, axisTextPaint);

        for (int i = 0; i < data.Count; i++)
        {
            double val = data[i].Value;
            string label = data[i].Label;
            double cx = padL + step * i + step / 2.0;
            double barH = Math.Abs(val) / maxAbs * (chartH / 2.0);
            float x = (float)(cx - barW / 2.0);

            bool isLeft = val >= 0;
            SKColor fillColor = isLeft
                ? SKColor.Parse("#3b82f6").WithAlpha(150)
                : SKColor.Parse("#f59e0b").WithAlpha(150);
            SKColor strokeColor = isLeft
                ? SKColor.Parse("#3b82f6")
                : SKColor.Parse("#f59e0b");
            SKColor labelColor = isLeft
                ? (isDarkTheme ? SKColor.Parse("#60a5fa") : SKColor.Parse("#2563eb"))
                : (isDarkTheme ? SKColor.Parse("#fbbf24") : SKColor.Parse("#d97706"));

            float barTop = isLeft ? (float)(zeroY - barH) : (float)zeroY;
            float barHeight = (float)Math.Max(barH, 2);

            using var fillPaint = new SKPaint { Color = fillColor, IsAntialias = true };
            using var edgePaint = new SKPaint
            {
                Color = strokeColor,
                StrokeWidth = 1.5f,
                IsAntialias = true,
                IsStroke = true
            };

            float r = 3f;
            using var path = new SKPath();

            if (isLeft)
            {
                path.MoveTo(x, barTop + r);
                path.QuadTo(x, barTop, x + r, barTop);
                path.LineTo(x + (float)barW - r, barTop);
                path.QuadTo(x + (float)barW, barTop, x + (float)barW, barTop + r);
                path.LineTo(x + (float)barW, barTop + barHeight);
                path.LineTo(x, barTop + barHeight);
                path.Close();
            }
            else
            {
                path.MoveTo(x, barTop);
                path.LineTo(x + (float)barW, barTop);
                path.LineTo(x + (float)barW, barTop + barHeight - r);
                path.QuadTo(x + (float)barW, barTop + barHeight, x + (float)barW - r, barTop + barHeight);
                path.LineTo(x + r, barTop + barHeight);
                path.QuadTo(x, barTop + barHeight, x, barTop + barHeight - r);
                path.Close();
            }

            canvas.DrawPath(path, fillPaint);

            if (isLeft)
            {
                canvas.DrawLine(x, barTop + r, x, barTop + barHeight, edgePaint);
                canvas.DrawLine(x + (float)barW, barTop + r, x + (float)barW, barTop + barHeight, edgePaint);
                canvas.DrawLine(x + r, barTop, x + (float)barW - r, barTop, edgePaint);
            }
            else
            {
                canvas.DrawLine(x, barTop, x, barTop + barHeight - r, edgePaint);
                canvas.DrawLine(x + (float)barW, barTop, x + (float)barW, barTop + barHeight - r, edgePaint);
                canvas.DrawLine(x + r, barTop + barHeight, x + (float)barW - r, barTop + barHeight, edgePaint);
            }

            using var valTextPaint = new SKPaint
            {
                Color = labelColor,
                TextSize = 11,
                IsAntialias = true,
                FakeBoldText = true,
                TextAlign = SKTextAlign.Center
            };

            string valText = isLeft ? $"+{val:F1}%" : $"{val:F1}%";
            float textY = isLeft
                ? barTop - 5
                : barTop + barHeight + 13;

            canvas.DrawText(valText, (float)cx, textY, valTextPaint);

            using var dateTextPaint = new SKPaint
            {
                Color = textColor,
                TextSize = 12,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center
            };
            canvas.DrawText(label, (float)cx, H - 8, dateTextPaint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new System.IO.MemoryStream(encoded.ToArray());
        TrendChart.Source = new Avalonia.Media.Imaging.Bitmap(ms);
    }

    private void LoadSessions(Patient patient)
    {
        var sessions = SessionService.Instance.GetForPatient(patient.Id);
        SessionHistoryPanel.Children.Clear();

        if (sessions.Count == 0)
        {
            var emptyBorder = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new FluentAvalonia.UI.Controls.SymbolIcon { Symbol = FluentAvalonia.UI.Controls.Symbol.Important, FontSize = 14, Foreground = SolidColorBrush.Parse("#3b82f6") },
                        new TextBlock { Text = "Brak zapisanych sesji.", FontSize = 12, Foreground = SolidColorBrush.Parse("#60a5fa"), VerticalAlignment = VerticalAlignment.Center }
                    }
                }
            };
            emptyBorder.Bind(Border.BackgroundProperty, this.GetResourceObservable("SurfaceBrush"));
            emptyBorder.Bind(Border.BorderBrushProperty, this.GetResourceObservable("SurfaceBorderBrush"));
            SessionHistoryPanel.Children.Add(emptyBorder);
            return;
        }

        var toShow = new List<Session>(sessions);
        toShow.Sort((a, b) => b.Date.CompareTo(a.Date));
        if (toShow.Count > 10) toShow = toShow.GetRange(0, 10);

        foreach (var session in toShow)
        {
            var asymColor = Math.Abs(session.AsymmetryIndex) > 10 ? "#ef4444" : "#10b981";
            var asymBg = Math.Abs(session.AsymmetryIndex) > 10 ? "#11ef4444" : "#1110b981";
            var dominantSide = session.AsymmetryIndex > 0 ? "L mocniejsza" : "R mocniejsza";

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var icon = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(8),
                Background = SolidColorBrush.Parse("#113b82f6"),
                Margin = new Thickness(0, 0, 12, 0),
                Child = new FluentAvalonia.UI.Controls.SymbolIcon
                {
                    Symbol = FluentAvalonia.UI.Controls.Symbol.People,
                    FontSize = 14,
                    Foreground = SolidColorBrush.Parse("#3b82f6"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var nameBlock = new TextBlock
            {
                Text = session.ExerciseName,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            nameBlock.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextPrimaryBrush"));

            var asymmetryBadge = new Border
            {
                Background = SolidColorBrush.Parse(asymBg),
                BorderBrush = SolidColorBrush.Parse(asymColor),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = session.AsymmetryIndex == 0
                        ? "Asymetria: brak danych"
                        : $"Asymetria: {Math.Abs(session.AsymmetryIndex):F1}% • {dominantSide}",
                    FontSize = 11,
                    Foreground = SolidColorBrush.Parse(session.AsymmetryIndex == 0 ? "#888888" : asymColor),
                    FontWeight = FontWeight.Medium
                }
            };

            var dateBlock = new TextBlock
            {
                Text = session.Date.ToString("dd.MM.yyyy  HH:mm"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            dateBlock.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("TextSecondaryBrush"));

            var detailBtn = new Button
            {
                Background = SolidColorBrush.Parse("#113b82f6"),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 6),
                VerticalAlignment = VerticalAlignment.Center,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Children =
                    {
                        new FluentAvalonia.UI.Controls.SymbolIcon { Symbol = FluentAvalonia.UI.Controls.Symbol.Open, FontSize = 12, Foreground = SolidColorBrush.Parse("#3b82f6") },
                        new TextBlock { Text = "Szczegóły", FontSize = 12, Foreground = SolidColorBrush.Parse("#3b82f6"), VerticalAlignment = VerticalAlignment.Center }
                    }
                }
            };
            detailBtn.Click += (s, e) => { SessionSelected?.Invoke(this, session); };

            Grid.SetColumn(icon, 0);
            Grid.SetColumn(nameBlock, 1);
            Grid.SetColumn(asymmetryBadge, 2);
            Grid.SetColumn(dateBlock, 3);
            Grid.SetColumn(detailBtn, 4);

            grid.Children.Add(icon);
            grid.Children.Add(nameBlock);
            grid.Children.Add(asymmetryBadge);
            grid.Children.Add(dateBlock);
            grid.Children.Add(detailBtn);

            var rowBorder = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14, 10),
                Margin = new Thickness(0, 0, 0, 6),
                Child = grid
            };
            
            rowBorder.Bind(Border.BackgroundProperty, this.GetResourceObservable("SurfaceHoverBrush"));
            rowBorder.Bind(Border.BorderBrushProperty, this.GetResourceObservable("SurfaceBorderBrush"));
            
            SessionHistoryPanel.Children.Add(rowBorder);
        }
    }

    private void EnterEditMode()
    {
        _originalNotes = _patient?.Notes ?? "";
        NotesEditBox.Text = _originalNotes;
        NotesViewMode.IsVisible = false;
        NotesEditBox.IsVisible = true;
        EditNotesButton.IsVisible = false;
        SaveNotesButton.IsVisible = true;
        CancelEditButton.IsVisible = true;
        NotesEditBox.Focus();
    }

    private void ExitEditMode(bool save)
    {
        if (save && _patient != null)
        {
            _patient.Notes = NotesEditBox.Text ?? "";
            PatientService.Instance.Save();
            var notes = _patient.Notes;
            NotesViewText.Text = string.IsNullOrWhiteSpace(notes)
                ? "Brak notatek. Kliknij 'Edytuj' aby dodać."
                : notes;
                
            string brushName = string.IsNullOrWhiteSpace(notes) ? "TextSecondaryBrush" : "TextPrimaryBrush";
            NotesViewText.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable(brushName));
        }
        NotesViewMode.IsVisible = true;
        NotesEditBox.IsVisible = false;
        EditNotesButton.IsVisible = true;
        SaveNotesButton.IsVisible = false;
        CancelEditButton.IsVisible = false;
    }
}