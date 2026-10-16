using static F.Configurator.Dsl.Tests.Declarations;

namespace F.Configurator.Dsl.Tests;

// Grammar 4.5 and 4.9: a collection gives the wizard its stages and features, and holds its own tables, computed
// features, rules and index.
public class CollectionParserTests
{
    [Fact]
    public void Collection_has_a_name_and_properties()
    {
        const string Source = """
            kolekcja Basic
              nazwa "Skrzydła płytowe Basic"
              tryb kolejny
            """;

        Assert.Equal("(kolekcja Basic (nazwa \"Skrzydła płytowe Basic\") (tryb kolejny))", Tree(Source));
    }

    [Fact]
    public void Stage_lists_its_features_in_order()
    {
        const string Source = """
            kolekcja Basic
              etap "Model"
                cechy Model, Typ
              etap "Wymiary"
                cechy Norma, Szerokosc,
                      SzerokoscMM
                cechy WysokoscMM
            """;

        Assert.Equal(
            "(kolekcja Basic (etap \"Model\" Model Typ) (etap \"Wymiary\" Norma Szerokosc SzerokoscMM WysokoscMM))",
            Tree(Source));
    }

    [Fact]
    public void Feature_may_be_visible_only_when_a_condition_holds()
    {
        const string Source = """
            kolekcja Basic
              etap "Okucia"
                cechy ZamekDolny, ZmianaPolozeniaZamka widoczna gdy ZamekDolny ≠ BRAK, Uchwyt
            """;

        Assert.Equal(
            "(kolekcja Basic (etap \"Okucia\" ZamekDolny (ZmianaPolozeniaZamka widoczna (≠ ZamekDolny BRAK)) Uchwyt))",
            Tree(Source));
    }

    [Fact]
    public void Features_may_be_listed_without_stages()
    {
        const string Source = """
            kolekcja Demo
              cechy Model, Norma
            """;

        Assert.Equal("(kolekcja Demo (bez etapu Model Norma))", Tree(Source));
    }

    [Fact]
    public void Collection_may_be_made_of_components()
    {
        const string Source = """
            kolekcja DrzwiDwuskrzydlowe
              komponent Czynne : Basic
              komponent Bierne : Basic
              wspólne Model, Kolor
              własne Szerokosc, ZamekDolny
            """;

        Assert.Equal(
            "(kolekcja DrzwiDwuskrzydlowe (komponent Czynne Basic) (komponent Bierne Basic) (cechy wspólne Model Kolor) "
            + "(cechy własne Szerokosc ZamekDolny))",
            Tree(Source));
    }

    [Fact]
    public void Collection_holds_its_own_tables_computed_features_and_rules()
    {
        const string Source = """
            kolekcja Demo
              tabela WymiaryDomyslne z "dane/wymiary.csv"
                klucz Norma
                wartości SzerokoscMM
              wyliczana Szersze = SzerokoscMM - 800
              reguła Wymiary
                wtedy domyślnie SzerokoscMM z WymiaryDomyslne
            """;

        Assert.Equal(
            "(kolekcja Demo (tabela WymiaryDomyslne \"dane/wymiary.csv\" (klucz Norma) (wartości SzerokoscMM)) "
            + "(wyliczana Szersze (- SzerokoscMM 800)) (reguła Wymiary (domyślnie SzerokoscMM z WymiaryDomyslne)))",
            Tree(Source));
    }

    [Fact]
    public void Index_is_one_expression_that_may_go_on_in_deeper_lines()
    {
        const string Source = """
            kolekcja Demo
              indeks
                Model + Norma + dopełnij(WysokoscMM, 4)
                + (jeśli wybrana(Zmiana) to "Z" + Zmiana inaczej "")
            """;

        Assert.Equal(
            "(kolekcja Demo (indeks (+ (+ (+ Model Norma) (dopełnij WysokoscMM 4)) "
            + "(jeśli (wybrana Zmiana) (+ \"Z\" Zmiana) \"\"))))",
            Tree(Source));
    }
}
