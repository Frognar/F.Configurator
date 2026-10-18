using F.Configurator.Catalog;
using F.Configurator.Expressions;
using static F.Configurator.Dsl.Tests.Compiling;

namespace F.Configurator.Dsl.Tests;

// Grammar 4.2: a choice feature declares the attributes of its options; each option gives its values, written under
// it or in a column of the options file.
public class OptionCompilerTests
{
    private static CatalogBuilder Demo() => CatalogBuilder.Create("Demo");

    private const string Models = "dane/modele.csv";

    private static ProductCatalog FromFile(string attributes, string csv) =>
        Library($"cecha Model : wybór\n{attributes}  opcje z \"{Models}\"", (Models, csv));

    private static IReadOnlyList<Diagnostic> FileDiagnostics(string attributes, string csv) =>
        LibraryDiagnostics($"cecha Model : wybór\n{attributes}  opcje z \"{Models}\"", (Models, csv));

    // Decision E5 (2026-10-14): a tak/nie value is the option TAK or NIE, as for a tak/nie feature.
    [Fact]
    public void Option_gives_values_to_attributes()
    {
        const string Source = """
            cecha Model : wybór
              atrybut Seria : tekst
              atrybut MaRamiak : tak/nie
              atrybut GruboscMM : liczba
              opcja PORTA "Porta"
                Seria ASTORIA
                MaRamiak TAK
                GruboscMM 40
              opcja VERTE
                Seria "13-VRATA D,P"
            """;

        Assert.Equal(
            Demo().Choice("Model", f => f
                .Attribute("Seria", AttributeType.Text)
                .Attribute("MaRamiak", AttributeType.Boolean)
                .Attribute("GruboscMM", AttributeType.Number)
                .Option("PORTA", "Porta", o => o
                    .Attribute("Seria", Value.Text("ASTORIA"))
                    .Attribute("MaRamiak", Value.Option("TAK"))
                    .Attribute("GruboscMM", Value.Number(40)))
                .Option("VERTE", "VERTE", o => o.Attribute("Seria", Value.Text("13-VRATA D,P")))).Build(),
            Library(Source));
    }

    [Fact]
    public void Number_given_to_a_text_attribute_is_text()
    {
        Assert.Equal(
            Demo().Choice("Model", f => f
                .Attribute("Szerokosc", AttributeType.Text)
                .Option("A", "A", o => o.Attribute("Szerokosc", Value.Text("80")))).Build(),
            Library("cecha Model : wybór\n  atrybut Szerokosc : tekst\n  opcja A\n    Szerokosc 80"));
    }

    [Fact]
    public void Attribute_default_fills_options_without_a_value()
    {
        const string Source = """
            cecha Model : wybór
              atrybut MaRamiak : tak/nie domyślnie NIE
              opcja PORTA
              opcja VERTE
                MaRamiak TAK
            """;

        Assert.Equal(
            Demo().Choice("Model", f => f
                .Attribute("MaRamiak", AttributeType.Boolean, Value.Option("NIE"))
                .Option("PORTA", "PORTA", o => o.Attribute("MaRamiak", Value.Option("NIE")))
                .Option("VERTE", "VERTE", o => o.Attribute("MaRamiak", Value.Option("TAK")))).Build(),
            Library(Source));
    }

    [Theory]
    [InlineData("GruboscMM dużo", 15, 4, "atrybut „GruboscMM” jest liczbą, a „dużo” nie")]
    [InlineData("MaRamiak 1", 14, 1, "atrybut „MaRamiak” przyjmuje TAK albo NIE")]
    [InlineData("Kolor BIALY", 5, 5, "„Kolor” nie jest atrybutem cechy „Model”")]
    [InlineData("symbol K", 12, 1, "po „symbol” oczekiwano tekstu w cudzysłowie")]
    [InlineData("Seria 1..2", 11, 4, "atrybut „Seria” przyjmuje nazwę, tekst albo liczbę")]
    [InlineData("Seria A, B", 14, 1, "atrybut „Seria” ma jedną wartość")]
    public void Wrong_value_of_an_option_is_an_error(string line, int column, int length, string message)
    {
        const string Attributes = "  atrybut MaRamiak : tak/nie\n  atrybut GruboscMM : liczba\n  atrybut Seria : tekst\n";
        var source = $"cecha Model : wybór\n{Attributes}  opcja A\n    {line}";

        Assert.Equal([Error(In(LibraryFile, 6, column, length), message)], LibraryDiagnostics(source));
    }

    [Fact]
    public void Wrong_default_of_an_attribute_is_an_error()
    {
        Assert.Equal(
            [Error(In(LibraryFile, 2, 40, 3), "atrybut „GruboscMM” jest liczbą, a „TAK” nie")],
            LibraryDiagnostics("cecha Model : wybór\n  atrybut GruboscMM : liczba domyślnie TAK"));
    }

    [Fact]
    public void Options_come_from_a_file()
    {
        const string Csv = """
            id;symbol;nazwa;obraz;Seria
            PORTA;P1;Porta;modele/porta.png;ASTORIA
            VERTE;V2;Verte;;KOLOR
            """;

        Assert.Equal(
            Demo().Choice("Model", f => f
                .Attribute("Seria", AttributeType.Text)
                .Option("PORTA", "Porta", o => o.Symbol("P1").Image("modele/porta.png").Attribute("Seria", Value.Text("ASTORIA")))
                .Option("VERTE", "Verte", o => o.Symbol("V2").Attribute("Seria", Value.Text("KOLOR")))).Build(),
            FromFile("  atrybut Seria : tekst\n", Csv));
    }

    [Fact]
    public void Values_in_a_file_have_the_types_of_their_attributes()
    {
        Assert.Equal(
            Demo().Choice("Model", f => f
                .Attribute("GruboscMM", AttributeType.Number)
                .Attribute("MaRamiak", AttributeType.Boolean)
                .Option("PORTA", "PORTA", o => o.Attribute("GruboscMM", Value.Number(40.5m)).Attribute("MaRamiak", Value.Option("TAK")))).Build(),
            FromFile("  atrybut GruboscMM : liczba\n  atrybut MaRamiak : tak/nie\n", "id;GruboscMM;MaRamiak\nPORTA;40.5;TAK"));
    }

    [Fact]
    public void Only_the_id_column_is_needed()
    {
        Assert.Equal(
            Demo().Choice("Model", f => f.Option("PORTA", "PORTA").Option("VERTE", "VERTE")).Build(),
            FromFile("", "id\nPORTA\nVERTE"));
    }

    // Decision G11 (Frognar, 2026-10-18): the `erp` column is the code of the option in the ERP, for the adapter.
    [Fact]
    public void Erp_column_is_the_external_code_of_the_option()
    {
        Assert.Equal(
            Demo().Choice("Model", f => f.Option("PORTA", "PORTA", o => o.Symbol("p1").ExternalCode("421"))).Build(),
            FromFile("", "id;symbol;erp\nPORTA;p1;421"));
    }

    // An empty cell is no value, as an empty text is (decision E4.9); the attribute's default fills it.
    [Fact]
    public void Empty_cell_gives_no_value()
    {
        Assert.Equal(
            Demo().Choice("Model", f => f
                .Attribute("Grupa", AttributeType.Text, Value.Text("BRAK"))
                .Attribute("Rozmiar", AttributeType.Text)
                .Option("PORTA", "PORTA", o => o.Attribute("Grupa", Value.Text("BRAK")))).Build(),
            FromFile("  atrybut Grupa : tekst domyślnie \"BRAK\"\n  atrybut Rozmiar : tekst\n", "id;Grupa;Rozmiar\nPORTA;;"));
    }

    [Fact]
    public void Option_with_an_empty_name_cell_is_named_by_its_id()
    {
        Assert.Equal(
            Demo().Choice("Model", f => f.Option("PORTA", "PORTA", o => o.Symbol("p1"))).Build(),
            FromFile("", "id;nazwa;symbol\nPORTA;;p1"));
    }

    [Fact]
    public void File_may_start_with_a_byte_order_mark_and_end_lines_with_CRLF()
    {
        Assert.Equal(
            Demo().Choice("Model", f => f.Option("PORTA", "PORTA", o => o.Symbol("p1"))).Build(),
            FromFile("", "﻿id;symbol\r\nPORTA;p1\r\n\r\n"));
    }

    // The syntax keeps the options written in place apart from the files, so their mutual order is not known.
    [Fact]
    public void Options_written_in_place_come_before_options_from_files()
    {
        const string Source = """
            cecha Model : wybór
              opcje z "dane/modele.csv"
              opcja BRAK
            """;

        Assert.Equal(
            Demo().Choice("Model", f => f.Option("BRAK", "BRAK").Option("PORTA", "PORTA")).Build(),
            Library(Source, (Models, "id\nPORTA")));
    }

    [Fact]
    public void Missing_options_file_is_an_error()
    {
        Assert.Equal(
            [Error(In(LibraryFile, 1, 7, 5), "brak pliku „dane/modele.csv”")],
            LibraryDiagnostics($"cecha Model : wybór\n  opcje z \"{Models}\""));
    }

    [Theory]
    [InlineData("", "", 1, 1, 0, "plik opcji jest pusty")]
    [InlineData("", "symbol;nazwa\nP1;Porta", 1, 1, 12, "w pliku opcji brak kolumny „id”")]
    [InlineData("", "id;symbol\n;p1", 2, 1, 0, "opcja bez id")]
    [InlineData("", "id;Kolor\nPORTA;BIALY", 1, 4, 5, "kolumna „Kolor” nie jest atrybutem cechy „Model”")]
    [InlineData("", "id;symbol;symbol\nPORTA;a;b", 1, 11, 6, "kolumna „symbol” jest już w pliku")]
    [InlineData("", "id;symbol\nPORTA;p1;x", 2, 1, 10, "wiersz ma 3 kolumny, a nagłówek 2")]
    [InlineData("", "id\nPORTA\nPORTA", 3, 1, 5, "opcja „PORTA” jest już w cesze „Model”")]
    [InlineData("  atrybut GruboscMM : liczba\n", "id;GruboscMM\nPORTA;dużo", 2, 7, 4, "atrybut „GruboscMM” jest liczbą, a „dużo” nie")]
    [InlineData("  atrybut GruboscMM : liczba\n", "id;GruboscMM\nPORTA;40,5", 2, 7, 4, "atrybut „GruboscMM” jest liczbą, a „40,5” nie")]
    [InlineData("  atrybut MaRamiak : tak/nie\n", "id;MaRamiak\nPORTA;1", 2, 7, 1, "atrybut „MaRamiak” przyjmuje TAK albo NIE")]
    public void Wrong_options_file_is_an_error(string attributes, string csv, int line, int column, int length, string message)
    {
        Assert.Equal([Error(In(Models, line, column, length), message)], FileDiagnostics(attributes, csv));
    }
}
