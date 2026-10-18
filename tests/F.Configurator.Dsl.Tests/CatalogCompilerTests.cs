using F.Configurator.Catalog;
using static F.Configurator.Dsl.Tests.Compiling;

namespace F.Configurator.Dsl.Tests;

// Grammar 4.1: the package has one `katalog`; its lines give the version, the base language and the reaction of rules
// without `przy naruszeniu`.
public class CatalogCompilerTests
{
    [Fact]
    public void Catalog_has_a_title_version_language_and_default_reaction()
    {
        const string Source = """
            katalog "Drzwi wewnętrzne"
              wersja "2026.10.1"
              język pl
              reakcja błąd
            """;

        var compilation = Compile((CatalogFile, Source));

        Assert.Empty(compilation.Diagnostics);
        Assert.Equal(
            CatalogBuilder.Create("Drzwi wewnętrzne").Version("2026.10.1").Language("pl").DefaultReaction(Reaction.Error).Build(),
            compilation.Catalog);
    }

    [Theory]
    [InlineData("koryguj", Reaction.Correct)]
    [InlineData("ostrzeż", Reaction.Warning)]
    [InlineData("blad", Reaction.Error)]
    public void Default_reaction_is_one_of_three(string reaction, Reaction expected)
    {
        var catalog = Compile((CatalogFile, $"katalog \"Demo\"\n  reakcja {reaction}")).Catalog;

        Assert.Equal(expected, catalog!.DefaultReaction);
    }

    // Files are read in the order of their paths, so the catalog does not depend on how the folder was listed.
    [Fact]
    public void Declarations_of_all_files_make_one_catalog()
    {
        var catalog = Compile(
            ("biblioteka/wymiary.kfg", "cecha SzerokoscMM : liczba"),
            (CatalogFile, "katalog \"Demo\""),
            ("biblioteka/okucia.kfg", "cecha Klamka : tekst")).Catalog;

        Assert.Equal(["Klamka", "SzerokoscMM"], catalog!.Features.Select(feature => feature.Name));
    }

    [Fact]
    public void Parse_errors_are_reported_and_there_is_no_catalog()
    {
        var compilation = Compile((CatalogFile, "katalog \"Demo\""), (LibraryFile, "cecha Model wybór"));

        Assert.Equal([Error(In(LibraryFile, 1, 13, 5), "brak „:”")], compilation.Diagnostics);
        Assert.Null(compilation.Catalog);
    }

    [Fact]
    public void Package_without_a_catalog_is_an_error()
    {
        var compilation = Compile((LibraryFile, "cecha Klamka : tekst"));

        Assert.Equal([Error(In("", 1, 1, 0), "pakiet nie ma deklaracji „katalog”")], compilation.Diagnostics);
        Assert.Null(compilation.Catalog);
    }

    [Fact]
    public void Second_catalog_is_an_error()
    {
        var compilation = Compile((CatalogFile, "katalog \"Demo\""), ("zapas.kfg", "katalog \"Inny\""));

        Assert.Equal([Error(In("zapas.kfg", 1, 9, 6), "pakiet ma już katalog „Demo”")], compilation.Diagnostics);
    }

    [Theory]
    [InlineData("kolor biały", 3, 5, "„kolor” nie jest właściwością katalogu (wersja, język, reakcja)")]
    [InlineData("reakcja popraw", 11, 6, "oczekiwano reakcji (koryguj, błąd, ostrzeż)")]
    [InlineData("wersja 2026", 10, 4, "oczekiwano tekstu w cudzysłowie")]
    [InlineData("język \"pl\"", 9, 4, "oczekiwano kodu języka, np. pl")]
    public void Wrong_catalog_line_is_an_error(string line, int column, int length, string message)
    {
        var compilation = Compile((CatalogFile, $"katalog \"Demo\"\n  {line}"));

        Assert.Equal([Error(In(CatalogFile, 2, column, length), message)], compilation.Diagnostics);
    }

    // Decision E5 (2026-10-14): the values of a context (the customers) come from the caller, so the catalog keeps
    // only its attributes and the file is not read.
    [Fact]
    public void Context_declares_its_attributes()
    {
        const string Source = """
            kontekst Kontrahent
              atrybut Asortyment : tekst
              wartości z "dane/kontrahenci.csv"
            """;

        Assert.Equal(
            CatalogBuilder.Create("Demo").Context("Kontrahent", c => c.Attribute("Asortyment", AttributeType.Text)).Build(),
            Library(Source));
    }

    [Theory]
    [InlineData("tekst", AttributeType.Text)]
    [InlineData("liczba", AttributeType.Number)]
    [InlineData("tak/nie", AttributeType.Boolean)]
    public void Context_attribute_has_its_type(string type, AttributeType expected)
    {
        var context = Library($"kontekst Kontrahent\n  atrybut Rabat : {type}").Contexts["Kontrahent"];

        Assert.Equal(expected, Assert.Single(context.Attributes).Type);
    }
}
