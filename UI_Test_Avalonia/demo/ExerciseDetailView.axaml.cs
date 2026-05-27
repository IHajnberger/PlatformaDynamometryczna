using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ScottPlot.Panels;
using System;

namespace UI_Test_Avalonia;

public partial class ExerciseDetailView : UserControl
{
    public event EventHandler? BackClicked;

    public ExerciseDetailView(string exerciseId)
    {
        InitializeComponent();

        BackButton.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        if (exerciseId == "CMJ")
            LoadCMJ();
        else
            LoadPlaceholder();
    }

    private void LoadCMJ()
    {
        TitleLabel.Text = "Skok pionowy (CMJ)";
        SubtitleLabel.Text = "Countermovement Jump • Test dynamometryczny";

        // Karta hero
        ContentPanel.Children.Add(MakeHeroCard(
            "Skok pionowy CMJ",
            "Countermovement Jump",
            "Podstawowy test oceny mocy eksplozywnej kończyn dolnych, szeroko stosowany w fizjoterapii sportowej i rehabilitacji.",
            "#1d3a6e", "#3b82f6", "Up"
        ));

        // Cel testu
        ContentPanel.Children.Add(MakeSectionCard(
            "Cel testu", "Target", "#3b82f6",
            "CMJ (Countermovement Jump) to standaryzowany test skoku pionowego z poprzedzającym ruchem ekscentrycznym (przysiadem). Pozwala ocenić:\n\n" +
            "• Moc eksplozywną kończyn dolnych\n" +
            "• Asymetrię sił między lewą a prawą nogą\n" +
            "• Efektywność cyklu rozciąganie–skurcz (SSC)\n" +
            "• Postęp rehabilitacji po urazach kończyn dolnych\n" +
            "• Gotowość sportową do powrotu do aktywności"
        ));

        // Co nam mówi
        ContentPanel.Children.Add(MakeSectionCard(
            "Co mówią nam wyniki?", "Important", "#10b981",
            "Platforma dynamometryczna rejestruje siły nacisku obu kończyn w czasie rzeczywistym. Na podstawie krzywej siła–czas obliczane są:\n\n" +
            "• Peak Force (siła szczytowa) – maksymalna siła generowana przez każdą kończynę osobno\n" +
            "• Flight Time (czas lotu) – czas przebywania w powietrzu, z którego wyliczana jest wysokość skoku\n" +
            "• Asymmetry Index – procentowa różnica obciążenia między kończynami; wartości >10% są klinicznie istotne\n" +
            "• Braking RFD – szybkość narastania siły w fazie hamowania, wskaźnik kontroli ekscentrycznej\n" +
            "• Load Ratio – stosunek obciążenia L:R podczas fazy lądowania"
        ));

        // Fazy
        ContentPanel.Children.Add(MakePhasesCard());

        // Jak wykonać
        ContentPanel.Children.Add(MakeSectionCard(
            "Jak prawidłowo wykonać test?", "Checkmark", "#f59e0b",
            "Przygotowanie:\n" +
            "• Pacjent stoi na platformie w pozycji wyprostowanej, stopy na szerokość bioder\n" +
            "• Ręce na biodrach przez cały czas trwania skoku (eliminacja kompensacji ramionami)\n" +
            "• Kilka minut rozgrzewki przed pierwszą próbą\n\n" +
            "Wykonanie:\n" +
            "• Na sygnał pacjent wykonuje szybki przysiad (ok. 90° w kolanie) i natychmiast wybija się maksymalnie w górę\n" +
            "• Lądowanie na obu stopach jednocześnie, w tym samym miejscu\n" +
            "• Kolana lekko ugięte podczas lądowania – nie na wyprostowanych nogach\n\n" +
            "Protokół:\n" +
            "• 3 próby z przerwą 60 sekund między skokami\n" +
            "• Do analizy brana jest próba z najwyższym Flight Time\n" +
            "• Wynik nieprawidłowy: asymetryczne odbicie, ręce odrywające się od bioder"
        ));

        // Normy
        ContentPanel.Children.Add(MakeNormsCard());
    }

    private void LoadPlaceholder()
    {
        TitleLabel.Text = "Skok z przysiadu (SQJ)";
        SubtitleLabel.Text = "Squat Jump • Test dynamometryczny";

        ContentPanel.Children.Add(MakeHeroCard(
            "Skok z przysiadu SQJ",
            "Squat Jump",
            "Opis tego testu będzie dostępny w kolejnej wersji aplikacji.",
            "#2d1b4e", "#8b5cf6", "CalendarDay"
        ));

        ContentPanel.Children.Add(MakeSectionCard(
            "Opis", "Important", "#8b5cf6",
            "Treść w przygotowaniu.\n\nSQJ (Squat Jump) to test skoku pionowego bez fazy ekscentrycznej – pacjent startuje ze statycznego przysiadu. Pozwala izolować komponent koncentryczny siły, bez udziału cyklu rozciąganie–skurcz."
        ));

        ContentPanel.Children.Add(new Border
        {
            Background = SolidColorBrush.Parse("#1c2333"),
            BorderBrush = SolidColorBrush.Parse("#2d3a55"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(12),
            Padding = new Avalonia.Thickness(20),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Children =
                {
                    new FluentAvalonia.UI.Controls.SymbolIcon { Symbol = FluentAvalonia.UI.Controls.Symbol.Clock, FontSize = 16, Foreground = SolidColorBrush.Parse("#3b82f6") },
                    new TextBlock { Text = "Pełna dokumentacja tego testu zostanie dodana w kolejnej wersji.", Foreground = SolidColorBrush.Parse("#60a5fa"), FontSize = 13, VerticalAlignment = VerticalAlignment.Center }
                }
            }
        });
    }

    private Border MakeHeroCard(string title, string subtitle, string desc, string bgColor, string accentColor, string icon)
    {
        return new Border
        {
            Background = SolidColorBrush.Parse("#1e1e1e"),
            BorderBrush = SolidColorBrush.Parse("#2d2d2d"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(16),
            Padding = new Avalonia.Thickness(28),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto, *"),
                Children =
                {
                    new Border
                    {
                        Width = 64, Height = 64,
                        CornerRadius = new Avalonia.CornerRadius(18),
                        Background = SolidColorBrush.Parse(bgColor),
                        Margin = new Avalonia.Thickness(0, 0, 20, 0),
                        VerticalAlignment = VerticalAlignment.Top,
                        Child = new FluentAvalonia.UI.Controls.SymbolIcon
                        {
                            Symbol = Enum.Parse<FluentAvalonia.UI.Controls.Symbol>(icon),
                            FontSize = 28,
                            Foreground = SolidColorBrush.Parse(accentColor),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }.Also(b => Grid.SetColumn(b, 0)),
                    new StackPanel
                    {
                        Spacing = 8,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeight.Bold, Foreground = Brushes.White },
                            new Border
                            {
                                Background = SolidColorBrush.Parse(bgColor),
                                CornerRadius = new Avalonia.CornerRadius(6),
                                Padding = new Avalonia.Thickness(10, 4),
                                HorizontalAlignment = HorizontalAlignment.Left,
                                Child = new TextBlock { Text = subtitle, FontSize = 12, Foreground = SolidColorBrush.Parse(accentColor) }
                            },
                            new TextBlock { Text = desc, FontSize = 13, Foreground = SolidColorBrush.Parse("#aaaaaa"), TextWrapping = TextWrapping.Wrap }
                        }
                    }.Also(sp => Grid.SetColumn(sp, 1))
                }
            }
        };
    }

    private Border MakeSectionCard(string title, string icon, string accentColor, string body)
    {
        return new Border
        {
            Background = SolidColorBrush.Parse("#2d2d2d"),
            BorderBrush = SolidColorBrush.Parse("#3d3d3d"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(16),
            Padding = new Avalonia.Thickness(24),
            Child = new StackPanel
            {
                Spacing = 14,
                Children =
                {
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 10,
                        Children =
                        {
                            new FluentAvalonia.UI.Controls.SymbolIcon
                            {
                                Symbol = Enum.Parse<FluentAvalonia.UI.Controls.Symbol>(icon),
                                FontSize = 18,
                                Foreground = SolidColorBrush.Parse(accentColor)
                            },
                            new TextBlock
                            {
                                Text = title,
                                FontSize = 15,
                                FontWeight = FontWeight.SemiBold,
                                Foreground = Brushes.White,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        }
                    },
                    new TextBlock
                    {
                        Text = body,
                        FontSize = 13,
                        Foreground = SolidColorBrush.Parse("#cccccc"),
                        TextWrapping = TextWrapping.Wrap,
                        LineHeight = 22
                    }
                }
            }
        };
    }

    private Border MakePhasesCard()
    {
        var phases = new[]
        {
            ("#3b82f6", "1", "Faza cicha (Quiet Standing)", "Pacjent stoi nieruchomo ~1s. Platforma kalibruje punkt zerowy i rejestruje masę ciała."),
            ("#f59e0b", "2", "Faza ekscentryczna (Unloading)", "Szybki przysiad – siła nacisku spada poniżej masy ciała. Mięśnie rozciągają się akumulując energię."),
            ("#ef4444", "3", "Faza koncentryczna (Propulsion)", "Gwałtowne wyprostowanie – Peak Force. Energia z fazy ekscentrycznej wyzwolona w ruchu w górę."),
            ("#10b981", "4", "Faza lotu (Flight)", "Brak nacisku na platformę. Flight Time → obliczana wysokość skoku H = g·t²/8."),
        };

        var panel = new StackPanel { Spacing = 10 };

        panel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Avalonia.Thickness(0, 0, 0, 4),
            Children =
            {
                new FluentAvalonia.UI.Controls.SymbolIcon { Symbol = FluentAvalonia.UI.Controls.Symbol.Flag, FontSize = 18, Foreground = SolidColorBrush.Parse("#f59e0b") },
                new TextBlock { Text = "Fazy testu CMJ", FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center }
            }
        });

        foreach (var (color, num, name, desc) in phases)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto, *"), Margin = new Avalonia.Thickness(0, 0, 0, 0) };

            var numBadge = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new Avalonia.CornerRadius(18),
                Background = SolidColorBrush.Parse(color + "33"),
                BorderBrush = SolidColorBrush.Parse(color),
                BorderThickness = new Avalonia.Thickness(1.5),
                Margin = new Avalonia.Thickness(0, 0, 14, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock { Text = num, FontSize = 14, FontWeight = FontWeight.Bold, Foreground = SolidColorBrush.Parse(color), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };

            var text = new StackPanel
            {
                Spacing = 3,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new TextBlock { Text = name, FontSize = 13, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White },
                    new TextBlock { Text = desc, FontSize = 12, Foreground = SolidColorBrush.Parse("#aaaaaa"), TextWrapping = TextWrapping.Wrap, LineHeight = 20 }
                }
            };

            Grid.SetColumn(numBadge, 0);
            Grid.SetColumn(text, 1);
            row.Children.Add(numBadge);
            row.Children.Add(text);

            panel.Children.Add(new Border
            {
                Background = SolidColorBrush.Parse("#1a1a1a"),
                BorderBrush = SolidColorBrush.Parse("#3d3d3d"),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(10),
                Padding = new Avalonia.Thickness(16, 12),
                Child = row
            });
        }

        return new Border
        {
            Background = SolidColorBrush.Parse("#2d2d2d"),
            BorderBrush = SolidColorBrush.Parse("#3d3d3d"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(16),
            Padding = new Avalonia.Thickness(24),
            Child = panel
        };
    }

    private Border MakeNormsCard()
    {
        var rows = new[]
        {
            ("Wysoki poziom", "> 40 cm", "#10b981"),
            ("Dobry poziom", "30–40 cm", "#3b82f6"),
            ("Przeciętny", "20–30 cm", "#f59e0b"),
            ("Wymaga pracy", "< 20 cm", "#ef4444"),
        };

        var panel = new StackPanel { Spacing = 10 };

        panel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Avalonia.Thickness(0, 0, 0, 4),
            Children =
            {
                new FluentAvalonia.UI.Controls.SymbolIcon { Symbol = FluentAvalonia.UI.Controls.Symbol.Ruler, FontSize = 18, Foreground = SolidColorBrush.Parse("#06b6d4") },
                new TextBlock { Text = "Orientacyjne normy (dorośli aktywni fizycznie)", FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center }
            }
        });

        foreach (var (label, value, color) in rows)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*, Auto") };
            row.Children.Add(new TextBlock { Text = label, FontSize = 13, Foreground = SolidColorBrush.Parse("#cccccc"), VerticalAlignment = VerticalAlignment.Center });
            var badge = new Border
            {
                Background = SolidColorBrush.Parse(color + "22"),
                BorderBrush = SolidColorBrush.Parse(color),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(10, 4),
                Child = new TextBlock { Text = value, FontSize = 13, FontWeight = FontWeight.Bold, Foreground = SolidColorBrush.Parse(color) }
            };
            Grid.SetColumn(badge, 1);
            row.Children.Add(badge);

            panel.Children.Add(new Border
            {
                Background = SolidColorBrush.Parse("#1a1a1a"),
                BorderBrush = SolidColorBrush.Parse("#3d3d3d"),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(10),
                Padding = new Avalonia.Thickness(16, 10),
                Child = row
            });
        }

        panel.Children.Add(new TextBlock
        {
            Text = "* Normy orientacyjne dla populacji ogólnej. Wartości mogą się różnić w zależności od płci, wieku i dyscypliny sportowej.",
            FontSize = 11,
            Foreground = SolidColorBrush.Parse("#555"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 6, 0, 0)
        });

        return new Border
        {
            Background = SolidColorBrush.Parse("#2d2d2d"),
            BorderBrush = SolidColorBrush.Parse("#3d3d3d"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(16),
            Padding = new Avalonia.Thickness(24),
            Child = panel
        };
    }
}

// Extension helper
public static class ControlExtensions
{
    public static T Also<T>(this T self, Action<T> block) { block(self); return self; }
}