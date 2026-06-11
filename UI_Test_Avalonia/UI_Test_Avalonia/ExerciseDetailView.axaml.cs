using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ScottPlot.Colormaps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UI_Test_Avalonia;

public partial class ExerciseDetailView : UserControl
{
    public event EventHandler? BackClicked;

    public ExerciseDetailView(string exerciseId)
    {
        InitializeComponent();

        BackButton.Click += (s, e) => BackClicked?.Invoke(this, EventArgs.Empty);

        switch (exerciseId)
        {
            case "SRC":
                LoadSRC();
                break;

            case "PJ":
                LoadPJ();
                break;

            case "TWiS":
                LoadTWiS();
                break;

            case "PO":
                LoadPO();
                break;

            case "PR":
                LoadPR();
                break;

            case "TWiI":
                LoadTWiI();
                break;

            case "TPC":
                LoadTPC();
                break;
        }
    }

    private void LoadSRC()
    {
        TitleLabel.Text = "Statyczny rozkład ciężaru";
        SubtitleLabel.Text = "Static Weight Distribution";

        // Karta hero
        ContentPanel.Children.Add(MakeHeroCard(
            "Statyczny rozkład ciężaru",
            "Static Weight Distribution",
            "Podstawowy test oceny symetrii obciążenia kończyn dolnych w pozycji stojącej. Umożliwia wykrywanie kompensacji, odciążania kończyny oraz zaburzeń postawy na podstawie rozkładu sił nacisku na platformy.",
            "#1d3a6e", "#3b82f6", "People"
        ));

        // Cel testu
        ContentPanel.Children.Add(MakeSectionCard(
            "Cel testu", "Target", "#3b82f6",

            "Test statycznego rozkładu ciężaru pozwala na szybką i obiektywną ocenę sposobu obciążania kończyn dolnych podczas spokojnego stania. Analiza umożliwia ocenę:\n\n" +
            "• Symetrii obciążenia lewej i prawej kończyny\n" +
            "• Naturalnych lub patologicznych kompensacji posturalnych\n" +
            "• Tendencji do odciążania kończyny po urazie lub zabiegu operacyjnym\n" +
            "• Skuteczności procesu rehabilitacji w przywracaniu równomiernego obciążenia\n" +
            "• Zmian w rozkładzie ciężaru wynikających z bólu, ograniczeń ruchomości lub zaburzeń równowagi"
        ));

        // Co nam mówi
        ContentPanel.Children.Add(MakeSectionCard(
            "Co mówią nam wyniki i wykresy?", "Important", "#10b981",

            "System rejestruje siły nacisku generowane przez każdą kończynę podczas spokojnego stania. Wyniki pozwalają ocenić zarówno wielkość obciążenia, jak i jego rozkład:\n\n" +
            "• Peak Force – najwyższa zarejestrowana wartość siły dla lewej i prawej kończyny\n" +
            "• Mean Force – średnie obciążenie kończyn podczas całego pomiaru\n" +
            "• Load Ratio – procentowy udział każdej kończyny w utrzymaniu masy ciała\n" +
            "• Asymmetry Index (ASI) – procentowa różnica obciążenia między stronami; wartość bliska 0% oznacza wysoką symetrię\n" +
            "• Total Force – całkowita siła nacisku generowana przez badanego podczas próby\n\n" +
            "Utrzymujące się asymetrie mogą świadczyć o kompensacjach bólowych, osłabieniu mięśniowym lub zaburzeniach kontroli posturalnej."
        ));

        // Fazy
        //ContentPanel.Children.Add(MakePhasesCard());

        // Jak wykonać
        ContentPanel.Children.Add(MakeSectionCard(
            "Jak prawidłowo wykonać test?", "Checkmark", "#f59e0b",

            "Przygotowanie:\n" +
            "• Pacjent staje swobodnie na dwóch platformach pomiarowych\n" +
            "• Każda stopa powinna znajdować się w całości na osobnej platformie\n" +
            "• Stopy ustawione równolegle na szerokość bioder\n" +
            "• Wzrok skierowany przed siebie, ręce opuszczone wzdłuż tułowia\n\n" +

            "Wykonanie:\n" +
            "• Pacjent pozostaje nieruchomo przez określony czas pomiaru\n" +
            "• Należy unikać świadomego przenoszenia ciężaru ciała między kończynami\n" +
            "• Nie wykonywać dodatkowych ruchów tułowiem ani kończynami górnymi\n\n" +

            "Protokół i zapis sesji:\n" +
            "• Zalecany czas pojedynczego pomiaru wynosi 10–30 sekund\n" +
            "• W razie potrzeby można wykonać kilka prób i uśrednić wyniki\n" +
            "• Badanie powinno być przeprowadzane w jednakowych warunkach podczas kolejnych wizyt kontrolnych\n" +
            "• Próba jest nieważna, jeśli pacjent wykona krok, oderwie stopę od platformy lub utraci równowagę"
        ));

        // Normy
        ContentPanel.Children.Add(MakeNormsCard(
        ExerciseParam.AsymmetryIndex, ExerciseParam.LoadRatio
        ));
    }
    private void LoadPJ()
    {
        TitleLabel.Text = "Próba jednonożna";
        SubtitleLabel.Text = "Single Leg Stance";

        ContentPanel.Children.Add(MakeHeroCard(
            "Próba jednonożna",
            "Single Leg Stance",
            "Test równowagi i kontroli posturalnej wykonywany na jednej kończynie dolnej. Pozwala ocenić stabilność, zdolność utrzymania środka ciężkości oraz jakość kontroli nerwowo-mięśniowej badanego.",
            "#2d1b4e", "#8b5cf6", "Star"
        ));

        ContentPanel.Children.Add(MakeSectionCard(
            "Cel testu", "Target", "#10b981",

            "Próba jednonożna służy do oceny zdolności utrzymania stabilnej pozycji stojącej na jednej kończynie. Test jest powszechnie wykorzystywany w rehabilitacji ortopedycznej, neurologicznej oraz sportowej. Pozwala ocenić:\n\n" +
            "• Kontrolę równowagi statycznej\n" +
            "• Stabilność stawu skokowego, kolanowego i biodrowego\n" +
            "• Funkcję układu proprioceptywnego\n" +
            "• Deficyty po urazach kończyn dolnych\n" +
            "• Skuteczność treningu stabilizacyjnego i procesu rehabilitacji\n" +
            "• Ryzyko ponownego urazu wynikające z zaburzeń kontroli posturalnej"
        ));

        ContentPanel.Children.Add(MakeSectionCard(
            "Co mówią nam wyniki i wykresy?", "Important", "#10b981",

            "Podczas testu analizowana jest zdolność utrzymania stabilnej pozycji na jednej kończynie. Parametry stabilności pozwalają określić jakość kontroli posturalnej:\n\n" +
            "• Stability Index – ogólny wskaźnik stabilności; niższe wartości oznaczają lepszą kontrolę równowagi\n" +
            "• Sway Velocity – prędkość wychyleń środka ciężkości; wysokie wartości wskazują na większą niestabilność\n" +
            "• Force Variability – zmienność nacisku na platformę w czasie próby\n" +
            "• Stabilization Time – czas potrzebny do osiągnięcia stabilnej pozycji po rozpoczęciu testu\n" +
            "• Mean Force – średnia siła nacisku rejestrowana podczas próby\n" +
            "• Asymmetry Index – różnice pomiędzy kończynami podczas wykonywania testu na lewej i prawej nodze\n\n" +
            "Wydłużony czas stabilizacji oraz zwiększone wychylenia często obserwuje się po skręceniach stawu skokowego, urazach ACL oraz u pacjentów z zaburzeniami propriocepcji."
        ));

        ContentPanel.Children.Add(MakeSectionCard(
            "Jak prawidłowo wykonać test?", "Checkmark", "#f59e0b",

            "Przygotowanie:\n" +
            "• Badany stoi na platformie pomiarowej obiema stopami\n" +
            "• Wzrok skierowany przed siebie\n" +
            "• Ramiona ułożone swobodnie wzdłuż tułowia\n\n" +

            "Wykonanie:\n" +
            "• Na sygnał pacjent unosi jedną kończynę nad podłoże\n" +
            "• Cały ciężar ciała przenoszony jest na kończynę badaną\n" +
            "• Należy utrzymać pozycję możliwie nieruchomo przez cały czas pomiaru\n" +
            "• Kończyna uniesiona nie może dotykać podłoża ani kończyny podporowej\n\n" +

            "Protokół i zapis sesji:\n" +
            "• Zalecany czas pojedynczej próby wynosi 20–30 sekund\n" +
            "• Test należy wykonać osobno dla lewej i prawej kończyny\n" +
            "• Dla większej wiarygodności można wykonać 2–3 powtórzenia\n" +
            "• Próba zostaje przerwana w przypadku utraty równowagi lub podparcia drugą nogą"
        ));

        ContentPanel.Children.Add(MakeNormsCard(
         ExerciseParam.AsymmetryIndex, ExerciseParam.StabilityIndex, ExerciseParam.SwayVelocity, ExerciseParam.StabilizationTime, ExerciseParam.ForceVariability
        ));


    }
    private void LoadTWiS()
    {
        TitleLabel.Text = "Test wstawania i siadania";
        SubtitleLabel.Text = "Sit To Stand Test";

        ContentPanel.Children.Add(MakeHeroCard(
            "Test wstawania i siadania",
            "Sit To Stand",
            "Funkcjonalny test oceniający zdolność generowania siły przez kończyny dolne podczas codziennego ruchu wstawania z pozycji siedzącej i powrotu do siadu. Pozwala analizować symetrię obciążenia, dynamikę ruchu oraz wydolność mięśniową pacjenta.",
            "#0c6e36", "#1ca657", "CalendarDay"
        ));

        ContentPanel.Children.Add(MakeSectionCard(
            "Cel testu", "Target", "#8b5cf6",

            "Test wstawania i siadania należy do najczęściej wykorzystywanych prób funkcjonalnych w fizjoterapii i rehabilitacji. Pozwala ocenić zdolność pacjenta do wykonywania podstawowych czynności życia codziennego oraz wykryć deficyty wpływające na sprawność ruchową.\n\n" +
            "Badanie umożliwia ocenę:\n\n" +
            "• Siły generowanej przez kończyny dolne podczas wstawania\n" +
            "• Symetrii obciążenia lewej i prawej strony ciała\n" +
            "• Szybkości transferu ciężaru podczas zmiany pozycji\n" +
            "• Dynamiki generowania siły (RFD)\n" +
            "• Wydolności mięśniowej podczas wykonywania serii powtórzeń\n" +
            "• Ograniczeń funkcjonalnych występujących po urazach, operacjach i w przebiegu chorób neurologicznych"
        ));

        ContentPanel.Children.Add(MakeSectionCard(
            "Co mówią nam wyniki i wykresy?", "Important", "#10b981",

            "Podczas testu system analizuje przebieg nacisku generowanego przez każdą kończynę w trakcie przechodzenia między pozycją siedzącą i stojącą.\n\n" +

            "• Peak Force – maksymalna siła wygenerowana przez każdą kończynę podczas ruchu\n" +
            "• Mean Force – średnia siła generowana podczas całej próby\n" +
            "• Asymmetry Index (ASI) – procentowa różnica obciążenia między stronami\n" +
            "• RFD (Rate of Force Development) – szybkość rozwijania siły; niski wynik może wskazywać na osłabienie mięśniowe lub ograniczenia funkcjonalne\n" +
            "• Time To Peak Force – czas potrzebny do osiągnięcia maksymalnej siły\n" +
            "• Weight Transfer Speed – szybkość przenoszenia ciężaru ciała pomiędzy kończynami\n" +
            "• Fatigue Index – zmiany parametrów siłowych pomiędzy kolejnymi powtórzeniami, pozwalające ocenić zmęczenie mięśniowe\n\n" +

            "Najczęściej obserwowanym objawem zaburzeń funkcjonalnych jest odciążanie jednej kończyny podczas wstawania, co prowadzi do wzrostu asymetrii oraz obniżenia generowanej siły."
        ));
        ContentPanel.Children.Add(MakeSectionCard(
            "Jak prawidłowo wykonać test?", "Checkmark", "#f59e0b",

            "Przygotowanie:\n" +
            "• Pacjent siada na stabilnym krześle ustawionym przed platformami pomiarowymi\n" +
            "• Stopy ustawione symetrycznie na platformach, na szerokość bioder\n" +
            "• Plecy wyprostowane, wzrok skierowany przed siebie\n" +
            "• Ręce skrzyżowane na klatce piersiowej lub oparte na barkach w celu ograniczenia pomocy kończyn górnych\n\n" +

            "Wykonanie:\n" +
            "• Na sygnał pacjent wstaje do pełnego wyprostu kończyn dolnych\n" +
            "• Następnie w sposób kontrolowany wraca do pozycji siedzącej\n" +
            "• Ruch powinien być wykonywany płynnie, bez podpierania się rękami\n" +
            "• Podczas całego testu należy utrzymywać stopy na platformach pomiarowych\n\n" +

            "Protokół i zapis sesji:\n" +
            "• Zaleca się wykonanie serii od 5 do 10 powtórzeń\n" +
            "• Wyniki analizowane są zarówno dla pojedynczych ruchów, jak i dla całej serii\n" +
            "• Test może być wykorzystywany do monitorowania postępów rehabilitacji oraz oceny wydolności mięśniowej\n" +
            "• Próba jest nieważna, jeśli pacjent pomaga sobie rękami lub przemieszcza stopy podczas pomiaru"
        ));

        ContentPanel.Children.Add(MakeNormsCard(
        ExerciseParam.AsymmetryIndex, ExerciseParam.WeightTransferSpeed, ExerciseParam.FatigueIndex
        ));
    }
    private void LoadPO()
    {
        TitleLabel.Text = "Przysiad";
        SubtitleLabel.Text = "Squat Assessment";

        ContentPanel.Children.Add(MakeHeroCard(
             "Przysiad",
             "Squat Assessment",
             "Funkcjonalny test oceny biomechaniki kończyn dolnych podczas wykonywania przysiadu. Pozwala analizować symetrię obciążenia, sposób generowania siły oraz strategie kompensacyjne pojawiające się w trakcie ruchu.",
             "#701e4b", "#b54a85", "Contact"
         ));

        ContentPanel.Children.Add(MakeSectionCard(
            "Cel testu", "Target", "#3b82f6",

            "Przysiad jest jednym z podstawowych wzorców ruchowych wykorzystywanych zarówno w codziennym funkcjonowaniu, jak i w sporcie. Analiza przysiadu na platformach pomiarowych umożliwia obiektywną ocenę sposobu obciążania kończyn dolnych podczas ruchu.\n\n" +

            "Badanie pozwala ocenić:\n\n" +
            "• Symetrię obciążenia lewej i prawej kończyny\n" +
            "• Strategie kompensacyjne występujące podczas ruchu\n" +
            "• Maksymalną i średnią siłę generowaną przez każdą kończynę\n" +
            "• Dynamikę rozwijania siły podczas fazy wstawania\n" +
            "• Deficyty funkcjonalne po urazach oraz zabiegach operacyjnych\n" +
            "• Gotowość pacjenta do powrotu do aktywności sportowej"
        ));

        ContentPanel.Children.Add(MakeSectionCard(
            "Co mówią nam wyniki i wykresy?", "Important", "#10b981",

            "System rejestruje zmiany siły nacisku pod każdą kończyną podczas całego cyklu przysiadu. Analiza przebiegu siły pozwala wykryć zarówno ograniczenia funkcjonalne, jak i nieprawidłowe wzorce ruchowe.\n\n" +

            "• Peak Force – maksymalna siła osiągnięta przez każdą kończynę podczas próby\n" +
            "• Mean Force – średnia wartość siły generowanej podczas ruchu\n" +
            "• Min Force – najniższa wartość siły, często związana z odciążeniem kończyny w określonej fazie ruchu\n" +
            "• Load Ratio – procentowy udział każdej kończyny w utrzymaniu ciężaru ciała\n" +
            "• Asymmetry Index (ASI) – procentowa różnica obciążenia pomiędzy stronami\n" +
            "• RFD (Rate of Force Development) – szybkość rozwijania siły podczas fazy wstawania\n" +
            "• Time To Peak Force – czas potrzebny do osiągnięcia maksymalnej siły\n\n" +

            "Najczęściej obserwowaną nieprawidłowością jest przesuwanie ciężaru ciała na kończynę zdrową, co skutkuje zwiększeniem asymetrii oraz zmniejszeniem generowanej siły po stronie osłabionej."
        ));

        ContentPanel.Children.Add(MakeSectionCard(
            "Jak prawidłowo wykonać test?", "Checkmark", "#f59e0b",

            "Przygotowanie:\n" +
            "• Pacjent staje na platformach pomiarowych, po jednej stopie na każdej platformie\n" +
            "• Stopy ustawione równolegle na szerokość bioder lub barków\n" +
            "• Tułów wyprostowany, wzrok skierowany przed siebie\n" +
            "• Ramiona skrzyżowane na klatce piersiowej lub oparte na biodrach\n\n" +

            "Wykonanie:\n" +
            "• Pacjent wykonuje kontrolowany przysiad do ustalonej głębokości\n" +
            "• Po osiągnięciu dolnej pozycji następuje płynny powrót do pełnego wyprostu\n" +
            "• Ruch powinien być wykonywany bez gwałtownych zmian tempa i bez utraty równowagi\n" +
            "• Stopy przez cały czas pozostają w kontakcie z platformami\n\n" +

            "Protokół i zapis sesji:\n" +
            "• Zaleca się wykonanie od 3 do 5 powtórzeń\n" +
            "• Wszystkie powtórzenia powinny być wykonywane w podobnym tempie\n" +
            "• Możliwe jest przeprowadzenie testu z masą własnego ciała lub dodatkowym obciążeniem\n" +
            "• Próba jest przerywana w przypadku utraty równowagi, oderwania stopy od platformy lub wystąpienia bólu"
        ));

        ContentPanel.Children.Add(MakeNormsCard(
            ExerciseParam.AsymmetryIndex, ExerciseParam.LoadRatio
        ));
    }
    private void LoadPR()
    {
        TitleLabel.Text = "Próba Romberga";
        SubtitleLabel.Text = "Balance Test • Romberg";

        // Karta hero
        ContentPanel.Children.Add(MakeHeroCard(
            "Próba Romberga",
            "Balance Test (Romberg)",
            "Klasyczny test równowagi służący do oceny kontroli posturalnej, stabilności oraz zdolności utrzymania środka ciężkości w obrębie pola podparcia. Pozwala wykrywać zaburzenia równowagi, asymetrie obciążenia i deficyty kontroli nerwowo-mięśniowej.",
            "#611d19", "#a34540", "Page"
        ));

        // Cel testu
        ContentPanel.Children.Add(MakeSectionCard(
            "Cel testu", "Target", "#3b82f6",
            "Próba Romberga umożliwia obiektywną ocenę stabilności posturalnej podczas stania w pozycji nieruchomej. Badanie pozwala ocenić:\n\n" +
            "• Zdolność utrzymania równowagi statycznej\n" +
            "• Efektywność działania układu proprioceptywnego\n" +
            "• Kompensacje wynikające z urazów kończyn dolnych\n" +
            "• Asymetryczne obciążanie lewej i prawej strony ciała\n" +
            "• Deficyty kontroli nerwowo-mięśniowej po zabiegach operacyjnych\n" +
            "• Ryzyko zaburzeń równowagi i upadków"
        ));

        // Co nam mówi
        ContentPanel.Children.Add(MakeSectionCard(
            "Co mówią nam wyniki i wykresy?", "Important", "#10b981",
            "Podczas testu analizowana jest stabilność nacisku wywieranego na platformy. Nawet niewielkie wahania środka ciężkości są rejestrowane i przedstawiane w postaci parametrów stabilności:\n\n" +
            "• Stability Index – ogólny wskaźnik stabilności; niższe wartości oznaczają lepszą kontrolę posturalną\n" +
            "• Sway Velocity – prędkość kołysania środka ciężkości; wzrost może świadczyć o zaburzeniach równowagi\n" +
            "• Force Variability – zmienność siły nacisku w czasie; duże wahania wskazują na trudności w utrzymaniu stabilnej pozycji\n" +
            "• Load Ratio – procentowy rozkład obciążenia pomiędzy kończynami\n" +
            "• Control Score – zbiorczy wskaźnik jakości kontroli posturalnej obliczany na podstawie wszystkich parametrów testu\n\n" +
            "U zdrowych osób wykresy pozostają względnie stabilne, a wahania środka ciężkości są niewielkie i regularne."
        ));

        // Jak wykonać
        ContentPanel.Children.Add(MakeSectionCard(
            "Jak prawidłowo wykonać test?", "Checkmark", "#f59e0b",
            "Przygotowanie:\n" +
            "• Pacjent stoi obunóż na platformach pomiarowych\n" +
            "• Stopy ustawione równolegle na szerokość bioder\n" +
            "• Ramiona swobodnie opuszczone wzdłuż tułowia\n" +
            "• Wzrok skierowany przed siebie na stały punkt\n\n" +
            "Wykonanie:\n" +
            "• Pacjent pozostaje nieruchomo przez określony czas (najczęściej 20–30 sekund)\n" +
            "• Należy unikać świadomego korygowania pozycji oraz wykonywania dodatkowych ruchów\n" +
            "• Test może być wykonywany z oczami otwartymi lub zamkniętymi w zależności od protokołu badania\n\n" +
            "Interpretacja:\n" +
            "• Zwiększone kołysanie świadczy o pogorszonej stabilności posturalnej\n" +
            "• Asymetryczny rozkład obciążenia może wskazywać na odciążanie jednej kończyny\n" +
            "• Wyniki należy analizować w odniesieniu do wieku, stanu klinicznego oraz wcześniejszych pomiarów pacjenta"
        ));

        ContentPanel.Children.Add(MakeNormsCard(
            ExerciseParam.SwayVelocity, ExerciseParam.LoadRatio, ExerciseParam.ForceVariability, ExerciseParam.ControlScore
        ));

    }
    private void LoadTWiI()
    {
        TitleLabel.Text = "Izometryczny półprzysiad";
        SubtitleLabel.Text = "Isometric HalfSquat";

        // Karta hero
        ContentPanel.Children.Add(MakeHeroCard(
            "Izometryczny półprzysiad",
            "Isometric Squat",
            "Test siły izometrycznej wykonywany w ustalonej pozycji półprzysiadu. Umożliwia ocenę zdolności generowania siły, symetrii obciążenia oraz odporności na zmęczenie bez wykonywania ruchu w stawach.",
            "#a66802", "#de9d2f", "List"
        ));

        // Cel testu
        ContentPanel.Children.Add(MakeSectionCard(
            "Cel testu", "Target", "#3b82f6",
            "Test izometrycznego półprzysiadu pozwala na ocenę zdolności układu mięśniowego do generowania oraz utrzymywania siły w warunkach statycznych. Badanie umożliwia ocenę:\n\n" +
            "• Maksymalnej siły generowanej przez kończyny dolne\n" +
            "• Asymetrii obciążenia pomiędzy stroną lewą i prawą\n" +
            "• Kontroli posturalnej podczas utrzymywania napięcia mięśniowego\n" +
            "• Zdolności do utrzymania stabilnej pozycji pod obciążeniem\n" +
            "• Występowania zmęczenia mięśniowego podczas wysiłku statycznego\n" +
            "• Postępów rehabilitacji po urazach i zabiegach operacyjnych"
        ));

        // Co nam mówi
        ContentPanel.Children.Add(MakeSectionCard(
            "Co mówią nam wyniki i wykresy?", "Important", "#10b981",
            "W trakcie testu analizowane są wartości siły generowane przez każdą kończynę oraz ich zmiany w czasie:\n\n" +
            "• Peak Force – maksymalna siła osiągnięta przez lewą i prawą kończynę\n" +
            "• Mean Force – średnia siła utrzymywana podczas całej próby\n" +
            "• RFD (Rate of Force Development) – szybkość narastania siły od rozpoczęcia napięcia mięśniowego\n" +
            "• Time To Peak Force – czas potrzebny do osiągnięcia maksymalnej siły\n" +
            "• Fatigue Index – stopień spadku generowanej siły w trakcie utrzymywania pozycji\n" +
            "• Control Score – zbiorcza ocena jakości wykonania testu oraz stabilności utrzymywanej pozycji\n\n" +
            "U osób zdrowych siła utrzymuje się na względnie stałym poziomie, a różnice pomiędzy kończynami pozostają niewielkie."
        ));

        // Jak wykonać
        ContentPanel.Children.Add(MakeSectionCard(
            "Jak prawidłowo wykonać test?", "Checkmark", "#f59e0b",
            "Przygotowanie:\n" +
            "• Pacjent stoi obunóż na platformach pomiarowych\n" +
            "• Stopy ustawione na szerokość bioder\n" +
            "• Ramiona skrzyżowane na klatce piersiowej lub oparte na biodrach\n" +
            "• Kolana ugięte do ustalonego kąta (najczęściej około 90°)\n\n" +
            "Wykonanie:\n" +
            "• Pacjent przyjmuje pozycję półprzysiadu i utrzymuje ją przez określony czas\n" +
            "• Należy zachować nieruchomą pozycję bez dodatkowych ruchów kompensacyjnych\n" +
            "• W trakcie próby rejestrowane są zmiany siły generowanej przez każdą kończynę\n\n" +
            "Interpretacja:\n" +
            "• Spadek siły w czasie wskazuje na rozwijające się zmęczenie mięśniowe\n" +
            "• Istotna asymetria może świadczyć o deficytach siłowych lub odciążaniu jednej kończyny\n" +
            "• Wydłużony czas osiągnięcia maksymalnej siły może wskazywać na zaburzenia aktywacji mięśniowej"
        ));

        ContentPanel.Children.Add(MakeNormsCard(
            ExerciseParam.FatigueIndex, ExerciseParam.ControlScore
        ));
    }
    private void LoadTPC()
    {
        TitleLabel.Text = "Test przenoszenia ciężaru";
        SubtitleLabel.Text = "Weight Shift Test";

        // Karta hero
        ContentPanel.Children.Add(MakeHeroCard(
            "Test przenoszenia ciężaru",
            "Weight Shift Test",
            "Dynamiczny test oceniający zdolność kontrolowanego przenoszenia ciężaru ciała pomiędzy kończynami dolnymi. Pozwala wykryć asymetrie obciążenia, kompensacje ruchowe oraz zaburzenia kontroli motorycznej.",
            "#0c4778", "#3c85c2", "Earth"
        ));

        // Cel testu
        ContentPanel.Children.Add(MakeSectionCard(
            "Cel testu", "Target", "#3b82f6",
            "Test przenoszenia ciężaru służy do oceny jakości transferu obciążenia pomiędzy kończynami dolnymi podczas kontrolowanego ruchu. Pozwala ocenić:\n\n" +
            "• Zdolność równomiernego obciążania obu kończyn\n" +
            "• Występowanie kompensacji po urazach i zabiegach operacyjnych\n" +
            "• Kontrolę nerwowo-mięśniową podczas zmiany punktu podparcia\n" +
            "• Szybkość oraz płynność transferu ciężaru ciała\n" +
            "• Deficyty funkcjonalne utrudniające powrót do codziennej aktywności lub sportu\n" +
            "• Zaufanie pacjenta do kończyny wcześniej objętej urazem"
        ));

        // Co nam mówi
        ContentPanel.Children.Add(MakeSectionCard(
            "Co mówią nam wyniki i wykresy?", "Important", "#10b981",
            "W trakcie testu analizowany jest sposób przemieszczania obciążenia pomiędzy platformami oraz reakcja organizmu na zmianę środka ciężkości:\n\n" +
            "• Peak Force – maksymalne obciążenie osiągane przez każdą kończynę podczas transferu\n" +
            "• Mean Force – średnia wartość obciążenia przypadająca na kończynę w trakcie próby\n" +
            "• Time To Peak Force – czas potrzebny do osiągnięcia maksymalnego obciążenia po rozpoczęciu ruchu\n" +
            "• RFD (Rate of Force Development) – szybkość budowania nacisku podczas przejmowania ciężaru ciała\n" +
            "• Fatigue Index – zmiany jakości wykonania w kolejnych powtórzeniach mogące wskazywać na zmęczenie\n" +
            "• Control Score – zbiorcza ocena płynności, kontroli i symetrii ruchu\n\n" +
            "U osób zdrowych transfer ciężaru przebiega płynnie, symetrycznie i bez widocznych opóźnień pomiędzy stronami."
        ));

        // Jak wykonać
        ContentPanel.Children.Add(MakeSectionCard(
            "Jak prawidłowo wykonać test?", "Checkmark", "#f59e0b",
            "Przygotowanie:\n" +
            "• Pacjent stoi obunóż na platformach pomiarowych\n" +
            "• Stopy ustawione równolegle na szerokość bioder\n" +
            "• Tułów utrzymywany w pozycji wyprostowanej\n" +
            "• Wzrok skierowany przed siebie\n\n" +
            "Wykonanie:\n" +
            "• Na sygnał pacjent przenosi ciężar ciała na jedną kończynę\n" +
            "• Po krótkim zatrzymaniu następuje kontrolowany transfer na stronę przeciwną\n" +
            "• Ruch powinien być płynny, bez gwałtownych szarpnięć oraz utraty równowagi\n" +
            "• Zaleca się wykonanie kilku powtórzeń w celu uzyskania reprezentatywnego wyniku\n\n" +
            "Interpretacja:\n" +
            "• Opóźnione przejmowanie obciążenia może świadczyć o deficytach kontroli ruchu\n" +
            "• Mniejsze obciążanie jednej kończyny często wskazuje na strategię ochronną po urazie\n" +
            "• Nagłe skoki siły mogą świadczyć o słabej kontroli motorycznej lub niestabilności posturalnej"
        ));

        ContentPanel.Children.Add(MakeNormsCard(
            ExerciseParam.LoadRatio, ExerciseParam.AsymmetryIndex, ExerciseParam.StabilizationTime, ExerciseParam.WeightTransferSpeed, ExerciseParam.FatigueIndex, ExerciseParam.ControlScore
        ));
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
    // nwm co z tym zrobić tbh
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
    // rozszerzona o poszczególne parametry
    private Border MakeNormsCard(params ExerciseParam[] parameters)
    {
        var panel = new StackPanel { Spacing = 10 };

        panel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Avalonia.Thickness(0, 0, 0, 4),
            Children =
        {
            new FluentAvalonia.UI.Controls.SymbolIcon
            {
                Symbol = FluentAvalonia.UI.Controls.Symbol.Ruler,
                FontSize = 18,
                Foreground = SolidColorBrush.Parse("#06b6d4")
            },
            new TextBlock
            {
                Text = "Normy i wartości referencyjne",
                FontSize = 15,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            }
        }
        });

        foreach (var parameter in parameters.Distinct())
        {
            var rows = GetNormRows(parameter);

            if (rows.Count == 0)
                continue;

            panel.Children.Add(new TextBlock
            {
                Text = GetParameterDisplayName(parameter),
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                Margin = new Avalonia.Thickness(0, 10, 0, 4)
            });

            foreach (var (label, value, color) in rows)
            {
                var row = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*, Auto")
                };

                row.Children.Add(new TextBlock
                {
                    Text = label,
                    FontSize = 13,
                    Foreground = SolidColorBrush.Parse("#cccccc"),
                    VerticalAlignment = VerticalAlignment.Center
                });

                var badge = new Border
                {
                    Background = SolidColorBrush.Parse(color + "22"),
                    BorderBrush = SolidColorBrush.Parse(color),
                    BorderThickness = new Avalonia.Thickness(1),
                    CornerRadius = new Avalonia.CornerRadius(6),
                    Padding = new Avalonia.Thickness(10, 4),
                    Child = new TextBlock
                    {
                        Text = value,
                        FontSize = 13,
                        FontWeight = FontWeight.Bold,
                        Foreground = SolidColorBrush.Parse(color)
                    }
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
        }

        panel.Children.Add(new TextBlock
        {
            Text = "* Wartości mają charakter orientacyjny i powinny być interpretowane w kontekście wieku, stanu zdrowia oraz celu terapii.",
            FontSize = 11,
            Foreground = SolidColorBrush.Parse("#555"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 8, 0, 0)
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
    private static string GetParameterDisplayName(ExerciseParam param)
    {
        return param switch
        {
            ExerciseParam.AsymmetryIndex => "Asymmetry Index (ASI)",
            ExerciseParam.LoadRatio => "Load Ratio",
            ExerciseParam.StabilityIndex => "Stability Index",
            ExerciseParam.SwayVelocity => "Sway Velocity",
            ExerciseParam.StabilizationTime => "Stabilization Time",
            ExerciseParam.ForceVariability => "Force Variability",
            ExerciseParam.ControlScore => "Control Score",
            ExerciseParam.FatigueIndex => "Fatigue Index",
            ExerciseParam.WeightTransferSpeed => "Weight Transfer Speed",
            _ => param.ToString()
        };
    }
    private static List<(string Label, string Value, string Color)> GetNormRows(ExerciseParam param)
    {
        return param switch
        {
            ExerciseParam.AsymmetryIndex => new()
        {
            ("Pełna symetria", "< 5%", "#10b981"),
            ("Asymetria fizjologiczna", "5–10%", "#3b82f6"),
            ("Asymetria istotna", "10–15%", "#f59e0b"),
            ("Deficyt kliniczny", "> 15%", "#ef4444")
        },

            ExerciseParam.LoadRatio => new()
        {
            ("Idealny rozkład", "50 / 50", "#10b981"),
            ("Akceptowalny", "45 / 55", "#3b82f6"),
            ("Istotna kompensacja", "40 / 60", "#f59e0b"),
            ("Znaczna kompensacja", "<40 / >60", "#ef4444")
        },

            ExerciseParam.StabilityIndex => new()
        {
            ("Bardzo dobra stabilność", "< 5", "#10b981"),
            ("Dobra stabilność", "5–10", "#3b82f6"),
            ("Umiarkowane zaburzenia", "10–15", "#f59e0b"),
            ("Znaczna niestabilność", "> 15", "#ef4444")
        },

            ExerciseParam.SwayVelocity => new()
        {
            ("Bardzo dobra kontrola", "< 10 mm/s", "#10b981"),
            ("Norma", "10–20 mm/s", "#3b82f6"),
            ("Pogorszona stabilność", "20–30 mm/s", "#f59e0b"),
            ("Znaczne zaburzenia", "> 30 mm/s", "#ef4444")
        },

            ExerciseParam.StabilizationTime => new()
        {
            ("Bardzo dobra kontrola", "< 2 s", "#10b981"),
            ("Norma", "2–5 s", "#3b82f6"),
            ("Spowolniona stabilizacja", "5–8 s", "#f59e0b"),
            ("Znaczne zaburzenia", "> 8 s", "#ef4444")
        },

            ExerciseParam.ForceVariability => new()
        {
            ("Stabilny nacisk", "< 5%", "#10b981"),
            ("Norma", "5–10%", "#3b82f6"),
            ("Podwyższona zmienność", "10–15%", "#f59e0b"),
            ("Niestabilna kontrola", "> 15%", "#ef4444")
        },

            ExerciseParam.ControlScore => new()
        {
            ("Doskonała kontrola", "90–100", "#10b981"),
            ("Dobra kontrola", "75–89", "#3b82f6"),
            ("Umiarkowane zaburzenia", "60–74", "#f59e0b"),
            ("Znaczne zaburzenia", "< 60", "#ef4444")
        },

            ExerciseParam.FatigueIndex => new()
        {
            ("Brak oznak zmęczenia", "< 5%", "#10b981"),
            ("Niewielkie zmęczenie", "5–10%", "#3b82f6"),
            ("Umiarkowane zmęczenie", "10–20%", "#f59e0b"),
            ("Znaczne zmęczenie", "> 20%", "#ef4444")
        },

            ExerciseParam.WeightTransferSpeed => new()
        {
            ("Płynny transfer", "Prawidłowy", "#10b981"),
            ("Nieznacznie opóźniony", "Akceptowalny", "#3b82f6"),
            ("Widoczna kompensacja", "Obserwacja", "#f59e0b"),
            ("Znaczne zaburzenia", "Interwencja", "#ef4444")
        },

            _ => new()
        };
    }
}

public static class ControlExtensions
{
    public static T Also<T>(this T self, Action<T> block) { block(self); return self; }
}