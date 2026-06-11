using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System;

namespace UI_Test_Avalonia;

public partial class ExerciseDetailView : UserControl
{
    public event EventHandler? BackClicked;

    public ExerciseDetailView(string exerciseId)
    {
        InitializeComponent();

        BackButton.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        // Zmienione ID z CMJ na SQ (Squat)
        if (exerciseId == "SQ")
            LoadSquat();
        else
            LoadPlaceholder();
    }

    private void LoadSquat()
    {
        TitleLabel.Text = "Przysiad (SQ)";
        SubtitleLabel.Text = "Squat Test • Ocena asymetrii i dystrybucji siły";

        // Karta hero
        ContentPanel.Children.Add(MakeHeroCard(
            "Test Przysiadu (SQ)",
            "Squat Test",
            "Podstawowy test oceny dystrybucji obciążenia i symetrii pracy kończyn dolnych w trakcie kontrolowanego przysiadu z lub bez obciążenia.",
            "#1d3a6e", "#3b82f6", "People"
        ));

        // Cel testu
        ContentPanel.Children.Add(MakeSectionCard(
            "Cel testu", "Target", "#3b82f6",
            "Test przysiadu na platformie dynamometrycznej pozwala na precyzyjną, obiektywną ocenę biomechaniki dolnego biokinematycznego łańcucha. Pozwala ocenić:\n\n" +
            "• Asymetrię sił (L vs R) generowanych podczas fazy ekscentrycznej i koncentrycznej\n" +
            "• Stabilność i kontrolę nerwowo-mięśniową pod obciążeniem\n" +
            "• Maksymalną siłę reakcji podłoża (Peak Force) dla każdej z kończyn\n" +
            "• Strategie kompensacyjne pacjenta po urazach i zabiegach operacyjnych\n" +
            "• Deficyty siłowe utrudniające powrót do pełnej sprawności (Return to Play)"
        ));

        // Co nam mówi
        ContentPanel.Children.Add(MakeSectionCard(
            "Co mówią nam wyniki i wykresy?", "Important", "#10b981",
            "Wykresy w czasie rzeczywistym rejestrują nacisk lewej i prawej nogi. Linia trendu oraz krzywa siły obrazują zmianę asymetrii w poszczególnych fazach ruchu:\n\n" +
            "• Wykres asymetrii (Trend) – pokazuje, czy pacjent przenosi ciężar ciała na jedną ze stron w miarę pogłębiania przysiadu lub w trakcie wstawania\n" +
            "• Peak Force (Siła szczytowa) – maksymalna wartość siły wygenerowana osobno przez lewą i prawą kończynę\n" +
            "• Asymmetry Index (ASI) – procentowa różnica obciążenia kończyn; linia trendu dąży do zera u pacjentów zdrowych, wartości stałe >10% świadczą o patologii\n" +
            "• Kontrola ekscentryczna – stabilność wykresu podczas schodzenia w dół, ujawniająca lęk przed obciążeniem chorej nogi"
        ));

        // Fazy
        ContentPanel.Children.Add(MakePhasesCard());

        // Jak wykonać
        ContentPanel.Children.Add(MakeSectionCard(
            "Jak prawidłowo wykonać test?", "Checkmark", "#f59e0b",
            "Przygotowanie:\n" +
            "• Pacjent stoi obunóż na platformach (jedna stopa na jednej platformie), pozycja wyprostowana, stopy na szerokość bioder\n" +
            "• Ramiona splecione na klatce piersiowej lub oparte na biodrach w celu eliminacji rotacji tułowia\n" +
            "• Ustalenie wariantu testu: z masą własnego ciała lub z zewnętrznym obciążeniem sztangą/hantlami\n\n" +
            "Wykonanie:\n" +
            "• Na sygnał pacjent wykonuje płynny, kontrolowany przysiad do określonego kąta (np. 90° w stawach kolanowych)\n" +
            "• Krótkie zatrzymanie w dolnej pozycji w celu stabilizacji odczytu\n" +
            "• Płynny powrót (wstanie) do pozycji wyjściowej\n\n" +
            "Protokół i zapis sesji:\n" +
            "• Wykonuje się serię 3-5 powtórzeń w celu wyznaczenia stabilnej linii trendu\n" +
            "• Placeholdery zapisu automatycznie kategoryzują sesję jako: Przysiad bez obciążenia / Przysiad z obciążeniem\n" +
            "• Próba jest odrzucana przy oderwaniu pięt od podłoża lub gwałtownym zachwianiu równowagi"
        ));

        // Normy
        ContentPanel.Children.Add(MakeNormsCard());
    }

    private void LoadPlaceholder()
    {
        TitleLabel.Text = "Przysiad izometryczny (ISO)";
        SubtitleLabel.Text = "Isometric Squat Test • Test dynamometryczny";

        ContentPanel.Children.Add(MakeHeroCard(
            "Przysiad izometryczny",
            "Isometric Squat",
            "Opis tego wariantu testu będzie dostępny w kolejnej wersji aplikacji.",
            "#2d1b4e", "#8b5cf6", "CalendarDay"
        ));

        ContentPanel.Children.Add(MakeSectionCard(
            "Opis", "Important", "#8b5cf6",
            "Treść w przygotowaniu.\n\nISO Squat to test maksymalnego skurczu izometrycznego w pozycji przysiadu pod stałym, zablokowanym oporem. Pozwala na bezpieczne badanie maksymalnej generowanej siły bez ruchu w stawach."
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
                    new TextBlock { Text = "Pełna dokumentacja testów izometrycznych zostanie dodana w kolejnej wersji.", Foreground = SolidColorBrush.Parse("#60a5fa"), FontSize = 13, VerticalAlignment = VerticalAlignment.Center }
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
        // Dostosowanie faz ruchu do przysiadu (usuwamy fazę lotu)
        var phases = new[]
        {
            ("#3b82f6", "1", "Faza początkowa (Nieruchome stanie)", "Pacjent stabilizuje pozycję na platformie. Rejestracja wagi wyjściowej jako punktu odniesienia."),
            ("#f59e0b", "2", "Faza ekscentryczna (Schodzenie)", "Ruch w dół. Analiza symetrii hamowania i płynności przenoszenia ciężaru ciała."),
            ("#ef4444", "3", "Faza izometryczna (Zatrzymanie)", "Utrzymanie dolnej pozycji (np. 90°). Najczęstszy moment ujawniania utrwalonych asymetrii."),
            ("#10b981", "4", "Faza koncentryczna (Wstawanie)", "Ruch w górę do pozycji wyprostowanej. Maksymalne zaangażowanie prostowników stawu kolanowego i biodrowego."),
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
                new TextBlock { Text = "Fazy analizy przysiadu (SQ)", FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center }
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
        // Zmiana norm ze skoku (w cm) na akceptowalny indeks asymetrii (w procentach)
        var rows = new[]
        {
            ("Pełna symetria (Norma)", "< 5%", "#10b981"),
            ("Asymetria fizjologiczna", "5% – 10%", "#3b82f6"),
            ("Asymetria istotna", "10% – 15%", "#f59e0b"),
            ("Kliniczny deficyt siłowy", "> 15%", "#ef4444"),
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
                new TextBlock { Text = "Normy indeksu asymetrii (ASI) dla przysiadu", FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center }
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
            Text = "* Wyrażone jako procentowa różnica obciążenia kończyny lewej i prawej. Wartości powyżej 10% w fazie koncentrycznej stabilnej są kluczowym wskaźnikiem do wdrożenia treningu jednostronnego.",
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

public static class ControlExtensions
{
    public static T Also<T>(this T self, Action<T> block) { block(self); return self; }
}