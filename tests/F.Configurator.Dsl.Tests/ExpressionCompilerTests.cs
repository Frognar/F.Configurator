using F.Configurator.Catalog;
using F.Configurator.Expressions;
using static F.Configurator.Dsl.Tests.Compiling;
using static F.Configurator.Expressions.Expression;

namespace F.Configurator.Dsl.Tests;

// Grammar 6: an expression becomes an engine expression. A name is a feature, a computed feature or a context; after
// a comparison with a choice it is an option of that choice.
public class ExpressionCompilerTests
{
    private const string Features = """
        cecha Model : wybór
          atrybut Seria : tekst
          atrybut MaRamiak : tak/nie domyślnie NIE
          opcja PORTA
            Seria ASTORIA
            MaRamiak TAK
          opcja VERTE
        cecha Norma : wybór
          opcja PL
          opcja CZ
          opcja SK
        cecha Szerokosc : wybór
          opcja "80"
          opcja "90"
        cecha SzerokoscMM : liczba
        cecha Grawer : tekst
        cecha Prog : tak/nie
        kontekst Kontrahent
          atrybut Asortyment : tekst
        """;

    // The line of `wyliczana W = …`; the expression starts in column 15.
    private static readonly int Line = Features.Split('\n').Length + 1;

    private static Expression Computed(string expression, string declarations = "") =>
        Assert.IsType<ComputedFeature>(Library($"{Features}\n{declarations}wyliczana W = {expression}").Features[^1]).Expression;

    private static IReadOnlyList<Diagnostic> Diagnostics(string expression) =>
        LibraryDiagnostics($"{Features}\nwyliczana W = {expression}");

    private static Expression SzerokoscMM => Reference("SzerokoscMM");

    [Fact]
    public void Number_and_text_are_literals()
    {
        Assert.Equal(Number(5), Computed("5"));
        Assert.Equal(Number(0.5m), Computed("0.5"));
        Assert.Equal(Text("T"), Computed("\"T\""));
    }

    [Fact]
    public void Minus_before_a_number_is_a_negative_number()
    {
        Assert.Equal(Number(-5), Computed("-5"));
    }

    [Fact]
    public void Feature_is_read_by_its_name()
    {
        Assert.Equal(SzerokoscMM, Computed("SzerokoscMM"));
    }

    [Fact]
    public void Computed_feature_is_read_by_its_name()
    {
        Assert.Equal(Reference("A"), Computed("A", "wyliczana A = 1\n"));
    }

    [Fact]
    public void Arithmetic_keeps_the_order_of_operators()
    {
        Assert.Equal(
            Add(Subtract(SzerokoscMM, Divide(Multiply(Number(4), Number(2)), Number(1))), Number(3)),
            Computed("SzerokoscMM - 4 * 2 / 1 + 3"));
    }

    [Fact]
    public void Minus_before_an_expression_negates_it()
    {
        Assert.Equal(Negate(SzerokoscMM), Computed("-SzerokoscMM"));
    }

    public static TheoryData<string, Expression> Comparisons => new()
    {
        { "SzerokoscMM = 5", Equal(SzerokoscMM, Number(5)) },
        { "SzerokoscMM ≠ 5", NotEqual(SzerokoscMM, Number(5)) },
        { "SzerokoscMM != 5", NotEqual(SzerokoscMM, Number(5)) },
        { "SzerokoscMM < 5", LessThan(SzerokoscMM, Number(5)) },
        { "SzerokoscMM <= 5", LessThanOrEqual(SzerokoscMM, Number(5)) },
        { "SzerokoscMM > 5", GreaterThan(SzerokoscMM, Number(5)) },
        { "SzerokoscMM >= 5", GreaterThanOrEqual(SzerokoscMM, Number(5)) },
    };

    [Theory]
    [MemberData(nameof(Comparisons))]
    public void Comparison_compares_two_values(string expression, Expression expected)
    {
        Assert.Equal(expected, Computed(expression));
    }

    [Fact]
    public void Name_compared_with_a_choice_is_its_option()
    {
        Assert.Equal(Equal(Reference("Norma"), Option("PL")), Computed("Norma = PL"));
        Assert.Equal(Equal(Option("PL"), Reference("Norma")), Computed("PL = Norma"));
        Assert.Equal(NotEqual(Reference("Norma"), Option("CZ")), Computed("Norma ≠ CZ"));
    }

    [Fact]
    public void Option_may_be_written_as_text_or_number()
    {
        Assert.Equal(Equal(Reference("Model"), Option("PORTA")), Computed("Model = \"PORTA\""));
        Assert.Equal(Equal(Reference("Szerokosc"), Option("80")), Computed("Szerokosc = 80"));
        Assert.Equal(Equal(Reference("Szerokosc"), Option("90")), Computed("Szerokosc = \"90\""));
    }

    [Fact]
    public void Yes_no_feature_compares_with_TAK_and_NIE()
    {
        Assert.Equal(Equal(Reference("Prog"), Option("TAK")), Computed("Prog = TAK"));
    }

    [Fact]
    public void Choice_may_be_in_a_list_of_options()
    {
        Assert.Equal(In(Reference("Norma"), List(Option("CZ"), Option("SK"))), Computed("Norma w [CZ, SK]"));
        Assert.Equal(NotIn(Reference("Norma"), List(Option("PL"))), Computed("Norma nie w [PL]"));
    }

    [Fact]
    public void Number_may_be_in_a_range()
    {
        Assert.Equal(In(SzerokoscMM, Range(600, 1100)), Computed("SzerokoscMM w 600..1100"));
        Assert.Equal(In(SzerokoscMM, Range(-5, -1)), Computed("SzerokoscMM w -5..-1"));
    }

    [Fact]
    public void Conditions_join_with_and_or_not()
    {
        Assert.Equal(
            Or(And(Equal(Reference("Norma"), Option("PL")), Not(Equal(Reference("Prog"), Option("TAK")))), HasValue(Reference("Grawer"))),
            Computed("Norma = PL i nie Prog = TAK lub wybrana(Grawer)"));
    }

    [Fact]
    public void Conditional_chooses_one_of_two_values()
    {
        Assert.Equal(
            If(Equal(Reference("Norma"), Option("PL")), Text("T"), Text("N")),
            Computed("jeśli Norma = PL to \"T\" inaczej \"N\""));
    }

    public static TheoryData<string, Expression> Functions => new()
    {
        { "domyślna(SzerokoscMM)", DefaultOf("SzerokoscMM") },
        { "wybrana(Grawer)", HasValue(Reference("Grawer")) },
        { "dopełnij(SzerokoscMM, 4)", Pad(SzerokoscMM, Number(4)) },
        { "dopełnij(Grawer, 6, \"x\")", Pad(Reference("Grawer"), Number(6), Text("x")) },
        { "zaokrąglij(SzerokoscMM)", Round(SzerokoscMM) },
        { "zaokrąglij(SzerokoscMM, 5)", Round(SzerokoscMM, Number(5)) },
        { "min(SzerokoscMM, 5, 7)", Min(SzerokoscMM, Number(5), Number(7)) },
        { "maks(SzerokoscMM, 5)", Max(SzerokoscMM, Number(5)) },
        { "długość(Grawer)", Length(Reference("Grawer")) },
    };

    [Theory]
    [MemberData(nameof(Functions))]
    public void Function_becomes_its_engine_expression(string expression, Expression expected)
    {
        Assert.Equal(expected, Computed(expression));
    }

    // Options without a value of the attribute are left out, so reading it gives no value.
    [Fact]
    public void Attribute_is_read_from_the_chosen_option()
    {
        Assert.Equal(
            OptionAttribute(Reference("Model"), new Dictionary<string, Value> { ["PORTA"] = Value.Text("ASTORIA") }.ToEquatableDictionary()),
            Computed("Model.Seria"));
    }

    [Fact]
    public void Attribute_default_is_read_for_options_without_a_value()
    {
        Assert.Equal(
            OptionAttribute(
                Reference("Model"),
                new Dictionary<string, Value> { ["PORTA"] = Value.Option("TAK"), ["VERTE"] = Value.Option("NIE") }.ToEquatableDictionary()),
            Computed("Model.MaRamiak"));
    }

    [Fact]
    public void Attribute_compares_with_a_name_of_its_type()
    {
        var series = OptionAttribute(
            Reference("Model"), new Dictionary<string, Value> { ["PORTA"] = Value.Text("ASTORIA") }.ToEquatableDictionary());
        var frame = OptionAttribute(
            Reference("Model"),
            new Dictionary<string, Value> { ["PORTA"] = Value.Option("TAK"), ["VERTE"] = Value.Option("NIE") }.ToEquatableDictionary());

        Assert.Equal(Equal(series, Text("ASTORIA")), Computed("Model.Seria = ASTORIA"));
        Assert.Equal(Equal(frame, Option("TAK")), Computed("Model.MaRamiak = TAK"));
    }

    // Decision E4.7: `Kontrahent` and `Język` are given by the platform and need no declaration.
    [Fact]
    public void Context_is_read_by_its_name()
    {
        Assert.Equal(ContextValue("Kontrahent", "Asortyment"), Computed("Kontrahent.Asortyment"));
        Assert.Equal(ContextValue("Kontrahent"), Computed("Kontrahent"));
        Assert.Equal(ContextValue("Język"), Computed("Język"));
    }

    [Theory]
    [InlineData("SzerokoscMM + Foo", 14, 3, "nieznana nazwa „Foo”")]
    [InlineData("Norma = PLL", 8, 3, "cecha „Norma” nie ma opcji „PLL”")]
    [InlineData("Norma w [PL, DE]", 13, 2, "cecha „Norma” nie ma opcji „DE”")]
    [InlineData("Szerokosc = 85", 12, 2, "cecha „Szerokosc” nie ma opcji „85”")]
    [InlineData("Prog = TAKK", 7, 4, "cecha „Prog” nie ma opcji „TAKK”")]
    [InlineData("Model.MaRamiak = 1", 17, 1, "atrybut „MaRamiak” przyjmuje TAK albo NIE")]
    [InlineData("Model.Kolor", 0, 11, "cecha „Model” nie ma atrybutu „Kolor”")]
    [InlineData("SzerokoscMM.X", 0, 13, "„SzerokoscMM” nie ma atrybutów, bo nie jest cechą typu wybór")]
    [InlineData("Kontrahent.Rabat", 0, 16, "kontekst „Kontrahent” nie ma atrybutu „Rabat”")]
    [InlineData("Język.Kod", 0, 9, "kontekst „Język” nie ma atrybutu „Kod”")]
    [InlineData("Foo.X", 0, 3, "nieznana nazwa „Foo”")]
    [InlineData("Model.Seria.X", 0, 13, "atrybut można odczytać tylko z cechy albo kontekstu")]
    [InlineData("SzerokoscMM w Grawer..5", 14, 9, "przedział ma liczby na obu końcach, np. 600..1100")]
    [InlineData("domyślna(5)", 9, 1, "„domyślna” przyjmuje nazwę cechy")]
    [InlineData("wybrana(Grawer, Prog)", 0, 21, "„wybrana” przyjmuje 1 argument")]
    [InlineData("długość(Grawer, 1)", 0, 18, "„długość” przyjmuje 1 argument")]
    [InlineData("dopełnij(Grawer)", 0, 16, "„dopełnij” przyjmuje 2 albo 3 argumenty")]
    [InlineData("zaokrąglij(SzerokoscMM, 1, 2)", 0, 29, "„zaokrąglij” przyjmuje 1 albo 2 argumenty")]
    [InlineData("1..2", 0, 4, "przedział może stać tylko po „w”")]
    [InlineData("Brak[Norma].X", 0, 11, "nieznana tabela „Brak”")]
    public void Wrong_expression_is_an_error(string expression, int offset, int length, string message)
    {
        Assert.Equal([Error(In(LibraryFile, Line, 15 + offset, length), message)], Diagnostics(expression));
    }

    [Fact]
    public void Options_of_a_choice_include_options_from_files()
    {
        var catalog = Library("cecha Kolor : wybór\n  opcje z \"dane/kolory.csv\"\nwyliczana W = Kolor = BIALY", ("dane/kolory.csv", "id\nBIALY"));

        Assert.Equal(new ComputedFeature("W", Equal(Reference("Kolor"), Option("BIALY"))), catalog.Features[^1]);
    }

    [Fact]
    public void Features_keep_the_order_of_declarations()
    {
        var catalog = Library("cecha A : liczba\nwyliczana W = A\ncecha B : liczba");

        Assert.Equal(["A", "W", "B"], catalog.Features.Select(feature => feature.Name));
    }

    [Fact]
    public void Computed_feature_of_a_collection_is_in_the_catalog()
    {
        var catalog = Library("cecha A : liczba\nkolekcja K\n  etap \"E\"\n    cechy A\n  wyliczana W = A + 1");

        Assert.Equal(new ComputedFeature("W", Add(Reference("A"), Number(1))), catalog.Features[^1]);
    }

    [Fact]
    public void Computed_feature_named_like_a_feature_is_an_error()
    {
        Assert.Equal(
            [Error(In(LibraryFile, 2, 11, 1), "cecha „A” jest już zadeklarowana")],
            LibraryDiagnostics("cecha A : liczba\nwyliczana A = 1"));
    }
}
