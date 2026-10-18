using F.Configurator.Catalog;
using static F.Configurator.Dsl.Tests.Compiling;

namespace F.Configurator.Dsl.Tests;

// Grammar 4.2: a feature becomes a catalog feature of its type with its options, limits and what the wizard shows.
public class FeatureCompilerTests
{
    private static CatalogBuilder Demo() => CatalogBuilder.Create("Demo");

    [Fact]
    public void Choice_feature_lists_its_options()
    {
        const string Source = """
            cecha Kierunek : wybór
              opcja L "Lewe"
              opcja P "Prawe"
            """;

        Assert.Equal(Demo().Choice("Kierunek", f => f.Option("L", "Lewe").Option("P", "Prawe")).Build(), Library(Source));
    }

    [Fact]
    public void Option_without_a_label_is_named_by_its_id()
    {
        const string Source = """
            cecha Szerokosc : wybór
              opcja "80"
              opcja BRAK
            """;

        Assert.Equal(Demo().Choice("Szerokosc", f => f.Option("80", "80").Option("BRAK", "BRAK")).Build(), Library(Source));
    }

    [Fact]
    public void Option_may_have_its_own_symbol_and_image()
    {
        const string Source = """
            cecha ZamekDolny : wybór
              opcja KLUCZ "Na klucz"
                symbol "K"
                obraz "ikony/zamek-klucz.svg"
            """;

        Assert.Equal(
            Demo().Choice("ZamekDolny", f => f.Option("KLUCZ", "Na klucz", o => o.Symbol("K").Image("ikony/zamek-klucz.svg"))).Build(),
            Library(Source));
    }

    // Decision E5 (2026-10-14): `tak/nie` is a choice of TAK and NIE, so it is compared and put in the index like
    // any other choice.
    [Fact]
    public void Yes_no_feature_is_a_choice_of_TAK_and_NIE()
    {
        Assert.Equal(
            Demo().Choice("Prog", f => f.Option("TAK", "TAK").Option("NIE", "NIE")).Build(),
            Library("cecha Prog : tak/nie"));
    }

    [Fact]
    public void Number_feature_has_its_unit_and_limits()
    {
        const string Source = """
            cecha SzerokoscMM : liczba
              jednostka mm
              min 600
              maks 1200
              krok 5
            """;

        Assert.Equal(
            Demo().Number("SzerokoscMM", f => f.Unit("mm").Min(600).Max(1200).Step(5)).Build(),
            Library(Source));
    }

    [Fact]
    public void Limits_may_be_negative()
    {
        Assert.Equal(
            Demo().Number("ZmianaPolozeniaZamka", f => f.Min(-50).Max(50)).Build(),
            Library("cecha ZmianaPolozeniaZamka : liczba\n  min -50\n  maks 50"));
    }

    [Fact]
    public void Text_feature_has_its_length()
    {
        Assert.Equal(Demo().Text("Grawer", f => f.Length(0, 20)).Build(), Library("cecha Grawer : tekst\n  długość 0..20"));
    }

    [Theory]
    [InlineData("znaki litery, cyfry", 5)]
    [InlineData("wzorzec \"[A-Z]+\"", 7)]
    public void Characters_and_pattern_of_a_text_are_not_checked_yet(string line, int length)
    {
        var compilation = CompileLibrary($"cecha Grawer : tekst\n  {line}");

        Assert.Equal(
            [Warning(In(LibraryFile, 2, 3, length), $"silnik jeszcze nie sprawdza „{line[..length]}”, pomijam")],
            compilation.Diagnostics);
        Assert.Equal(Demo().Text("Grawer").Build(), compilation.Catalog);
    }

    [Theory]
    [InlineData("tekst", "NIE", false)]
    [InlineData("tekst", "TAK", true)]
    [InlineData("liczba", "NIE", false)]
    [InlineData("wybór", "NIE", false)]
    [InlineData("tak/nie", "NIE", false)]
    public void Feature_may_be_optional(string type, string required, bool isRequired)
    {
        var feature = Assert.Single(Library($"cecha X : {type}\n  wymagana {required}").Features);

        Assert.Equal(isRequired, feature.IsRequired);
    }

    // Decision E5 (2026-10-14): the catalog keeps what the wizard shows; the engine does not use it.
    [Fact]
    public void Feature_has_a_label_description_presentation_and_image()
    {
        const string Source = """
            cecha Model : wybór
              nazwa "Model drzwi"
              opis "Wybierz wzór skrzydła"
              prezentacja kafelki
              obraz "modele.png"
              opcja PORTA
            """;

        Assert.Equal(
            Demo().Choice("Model", f => f
                .Metadata(new FeatureMetadata("Model drzwi", "Wybierz wzór skrzydła", Presentation.Tiles, "modele.png"))
                .Option("PORTA", "PORTA")).Build(),
            Library(Source));
    }

    [Theory]
    [InlineData("tekst")]
    [InlineData("tak/nie")]
    public void Every_feature_type_has_a_label(string type)
    {
        var feature = Assert.Single(Library($"cecha X : {type}\n  nazwa \"Iks\"").Features);

        Assert.Equal("Iks", feature.Metadata.Label);
    }

    [Theory]
    [InlineData("lista", Presentation.List)]
    [InlineData("przyciski", Presentation.Buttons)]
    [InlineData("kafelki", Presentation.Tiles)]
    [InlineData("próbki", Presentation.Swatches)]
    [InlineData("pole", Presentation.Field)]
    [InlineData("suwak", Presentation.Slider)]
    public void Presentation_is_one_of_six(string presentation, Presentation expected)
    {
        var feature = Assert.Single(Library($"cecha SzerokoscMM : liczba\n  prezentacja {presentation}").Features);

        Assert.Equal(expected, feature.Metadata.Presentation);
    }

    [Fact]
    public void Multiple_choice_is_not_supported_yet()
    {
        Assert.Equal(
            [Error(In(LibraryFile, 1, 7, 7), "„wielokrotny wybór” jeszcze nie działa w silniku")],
            LibraryDiagnostics("cecha Dodatki : wielokrotny wybór\n  opcja A"));
    }

    private const string ChoiceProperties = "(nazwa, opis, prezentacja, obraz, wymagana)";

    [Theory]
    [InlineData("wybór", "jednostka mm", 3, 9, $"„jednostka” nie jest właściwością cechy typu wybór {ChoiceProperties}")]
    [InlineData(
        "liczba", "długość 0..5", 3, 7,
        "„długość” nie jest właściwością cechy typu liczba (nazwa, opis, prezentacja, obraz, wymagana, jednostka, min, maks, krok)")]
    [InlineData(
        "tekst", "krok 5", 3, 4,
        "„krok” nie jest właściwością cechy typu tekst (nazwa, opis, prezentacja, obraz, wymagana, długość, znaki, wzorzec)")]
    [InlineData("tekst", "prezentacja kolorowa", 15, 8, "nieznana prezentacja „kolorowa” (lista, przyciski, kafelki, próbki, pole, suwak)")]
    [InlineData("tekst", "wymagana może", 12, 4, "po „wymagana” oczekiwano TAK albo NIE")]
    [InlineData("liczba", "krok dużo", 8, 4, "po „krok” oczekiwano liczby")]
    [InlineData("liczba", "jednostka \"mm\"", 13, 4, "po „jednostka” oczekiwano nazwy jednostki, np. mm")]
    [InlineData("tekst", "długość 5", 11, 1, "po „długość” oczekiwano przedziału, np. 0..20")]
    [InlineData("wybór", "nazwa Model", 9, 5, "po „nazwa” oczekiwano tekstu w cudzysłowie")]
    [InlineData("liczba", "atrybut A : tekst", 11, 1, "atrybuty mają tylko cechy typu wybór")]
    [InlineData("liczba", "opcja A", 9, 1, "opcje mają tylko cechy typu wybór")]
    [InlineData("tak/nie", "opcja A", 9, 1, "cecha tak/nie ma zawsze opcje TAK i NIE")]
    public void Wrong_feature_line_is_an_error(string type, string line, int column, int length, string message)
    {
        Assert.Equal(
            [Error(In(LibraryFile, 2, column, length), message)],
            LibraryDiagnostics($"cecha X : {type}\n  {line}"));
    }

    [Fact]
    public void Repeated_feature_is_an_error()
    {
        var compilation = CompileLibrary("cecha Grawer : tekst\ncecha Grawer : liczba");

        Assert.Equal([Error(In(LibraryFile, 2, 7, 6), "cecha „Grawer” jest już zadeklarowana")], compilation.Diagnostics);
        Assert.Null(compilation.Catalog);
    }

    [Fact]
    public void Repeated_option_is_an_error()
    {
        const string Source = """
            cecha Kierunek : wybór
              opcja L "Lewe"
              opcja L "Lewe otwierane"
            """;

        Assert.Equal([Error(In(LibraryFile, 3, 9, 1), "opcja „L” jest już w cesze „Kierunek”")], LibraryDiagnostics(Source));
    }
}
