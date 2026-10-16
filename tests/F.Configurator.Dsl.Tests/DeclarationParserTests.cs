using static F.Configurator.Dsl.Tests.Declarations;

namespace F.Configurator.Dsl.Tests;

// Grammar 4.1–4.6: the parser reads the declarations of a file into syntax. It knows the shape of each declaration
// but not the meaning of its properties: whether `prezentacja kafelki` is allowed, or what `wersja` is, is checked
// when the whole package is compiled.
public class DeclarationParserTests
{
    [Theory]
    [InlineData("")]
    [InlineData("# tylko komentarz\n\n")]
    public void File_without_declarations_has_none(string source)
    {
        Assert.Empty(Trees(source));
    }

    [Fact]
    public void Catalog_has_a_title()
    {
        Assert.Equal("(katalog \"Drzwi demo\")", Tree("katalog \"Drzwi demo\""));
    }

    [Fact]
    public void Catalog_lists_its_properties()
    {
        const string Source = """
            katalog "Drzwi demo"
              wersja "2026.10.1"
              język pl
            """;

        Assert.Equal("(katalog \"Drzwi demo\" (wersja \"2026.10.1\") (język pl))", Tree(Source));
    }

    [Fact]
    public void Keyword_alone_as_a_value_is_read_as_a_word()
    {
        const string Source = """
            katalog "Drzwi demo"
              reakcja błąd
            """;

        Assert.Equal("(katalog \"Drzwi demo\" (reakcja błąd))", Tree(Source));
    }

    [Fact]
    public void Feature_has_a_name_and_a_type()
    {
        Assert.Equal("(cecha SzerokoscMM liczba)", Tree("cecha SzerokoscMM : liczba"));
    }

    [Theory]
    [InlineData("wybór", "wybór")]
    [InlineData("wybor", "wybór")]
    [InlineData("wielokrotny wybór", "wielokrotny wybór")]
    [InlineData("tak/nie", "tak/nie")]
    [InlineData("liczba", "liczba")]
    [InlineData("tekst", "tekst")]
    public void Feature_type_is_one_of_five(string written, string type)
    {
        Assert.Equal($"(cecha X {type})", Tree($"cecha X : {written}"));
    }

    [Fact]
    public void Feature_lists_its_properties()
    {
        const string Source = """
            cecha SzerokoscMM : liczba
              nazwa "Szerokość [mm]"
              jednostka mm
              wymagana NIE
              krok 1
            """;

        Assert.Equal(
            "(cecha SzerokoscMM liczba (nazwa \"Szerokość [mm]\") (jednostka mm) (wymagana NIE) (krok 1))",
            Tree(Source));
    }

    [Fact]
    public void Property_may_have_several_values()
    {
        const string Source = """
            cecha Grawer : tekst
              znaki litery, cyfry, spacja, "-"
            """;

        Assert.Equal("(cecha Grawer tekst (znaki litery cyfry spacja \"-\"))", Tree(Source));
    }

    [Fact]
    public void Property_value_is_an_expression()
    {
        const string Source = """
            cecha Grawer : tekst
              długość 0..20
            """;

        Assert.Equal("(cecha Grawer tekst (długość (.. 0 20)))", Tree(Source));
    }

    [Fact]
    public void Attribute_has_a_name_and_a_type()
    {
        const string Source = """
            cecha Model : wybór
              atrybut Seria : tekst
            """;

        Assert.Equal("(cecha Model wybór (atrybut Seria tekst))", Tree(Source));
    }

    [Fact]
    public void Attribute_may_have_a_default()
    {
        const string Source = """
            cecha Model : wybór
              atrybut MaRamiak : tak/nie domyślnie NIE
            """;

        Assert.Equal("(cecha Model wybór (atrybut MaRamiak tak/nie NIE))", Tree(Source));
    }

    [Fact]
    public void Options_may_come_from_a_file()
    {
        const string Source = """
            cecha Model : wybór
              opcje z "dane/modele.csv"
            """;

        Assert.Equal("(cecha Model wybór (opcje \"dane/modele.csv\"))", Tree(Source));
    }

    [Fact]
    public void Option_has_an_id_and_a_label()
    {
        const string Source = """
            cecha Kierunek : wybór
              opcja L "Lewe"
              opcja P "Prawe"
            """;

        Assert.Equal("(cecha Kierunek wybór (opcja L \"Lewe\") (opcja P \"Prawe\"))", Tree(Source));
    }

    [Fact]
    public void Option_label_is_optional()
    {
        const string Source = """
            cecha Skosne : wybór
              opcja TAK
            """;

        Assert.Equal("(cecha Skosne wybór (opcja TAK))", Tree(Source));
    }

    [Fact]
    public void Option_id_may_be_written_in_quotes()
    {
        const string Source = """
            cecha Szerokosc : wybór
              opcja "80" "80 cm"
              opcja "13-VRATA D,P"
            """;

        Assert.Equal("(cecha Szerokosc wybór (opcja 80 \"80 cm\") (opcja 13-VRATA D,P))", Tree(Source));
    }

    [Fact]
    public void Option_lists_its_symbol_image_and_attribute_values()
    {
        const string Source = """
            cecha ZamekDolny : wybór
              atrybut Seria : tekst
              opcja KLUCZ "Na klucz"
                symbol "K"
                obraz "ikony/zamek-klucz.svg"
                Seria ASTORIA
            """;

        Assert.Equal(
            "(cecha ZamekDolny wybór (atrybut Seria tekst) "
            + "(opcja KLUCZ \"Na klucz\" (symbol \"K\") (obraz \"ikony/zamek-klucz.svg\") (Seria ASTORIA)))",
            Tree(Source));
    }

    [Fact]
    public void Feature_reads_the_lines_after_an_option_block()
    {
        const string Source = """
            cecha Skosne : wybór
              opcja TAK
                symbol "T"
              opcja NIE
                symbol "N"
              prezentacja przyciski
            """;

        Assert.Equal(
            "(cecha Skosne wybór (prezentacja przyciski) (opcja TAK (symbol \"T\")) (opcja NIE (symbol \"N\")))",
            Tree(Source));
    }

    [Fact]
    public void Computed_feature_is_a_name_and_an_expression()
    {
        Assert.Equal(
            "(wyliczana OdchylkaSzerokosc (- SzerokoscMM (domyślna SzerokoscMM)))",
            Tree("wyliczana OdchylkaSzerokosc = SzerokoscMM - domyślna(SzerokoscMM)"));
    }

    [Fact]
    public void Computed_expression_may_go_on_in_deeper_lines()
    {
        const string Source = """
            wyliczana Nietyp =
              jeśli SzerokoscMM ≠ domyślna(SzerokoscMM) lub WysokoscMM ≠ domyślna(WysokoscMM)
              to "T" inaczej "N"
            """;

        Assert.Equal(
            "(wyliczana Nietyp (jeśli (lub (≠ SzerokoscMM (domyślna SzerokoscMM)) (≠ WysokoscMM (domyślna WysokoscMM))) "
            + "\"T\" \"N\"))",
            Tree(Source));
    }

    [Fact]
    public void Table_from_a_file_names_its_key_and_value_columns()
    {
        const string Source = """
            tabela WymiaryDomyslne z "dane/basic-wymiary.csv"
              klucz Norma, Szerokosc
              wartości SzerokoscMM, WysokoscMM
            """;

        Assert.Equal(
            "(tabela WymiaryDomyslne \"dane/basic-wymiary.csv\" (klucz Norma Szerokosc) (wartości SzerokoscMM WysokoscMM))",
            Tree(Source));
    }

    [Fact]
    public void Table_of_allowed_combinations_names_the_allowed_feature()
    {
        const string Source = """
            tabela DozwoloneTyp z "dane/zaleznosci/typ.csv"
              klucz Model
              dozwolone Typ
            """;

        Assert.Equal("(tabela DozwoloneTyp \"dane/zaleznosci/typ.csv\" (klucz Model) (dozwolone Typ))", Tree(Source));
    }

    [Fact]
    public void Table_rows_may_be_written_in_place()
    {
        const string Source = """
            tabela WymiaryDomyslne
              klucz Norma, Szerokosc
              wartości SzerokoscMM, WysokoscMM
              | PL | "80" | 844 | 2030 |
              | PL | "90" | 944 | 2030 |
            """;

        Assert.Equal(
            "(tabela WymiaryDomyslne (klucz Norma Szerokosc) (wartości SzerokoscMM WysokoscMM) "
            + "(wiersz PL \"80\" 844 2030) (wiersz PL \"90\" 944 2030))",
            Tree(Source));
    }

    [Fact]
    public void Table_cell_may_hold_several_values_or_none()
    {
        const string Source = """
            tabela WymiaryDomyslne
              klucz Norma, Szerokosc
              wartości SzerokoscMM
              | CZ, SK | | 830 |
            """;

        Assert.Equal(
            "(tabela WymiaryDomyslne (klucz Norma Szerokosc) (wartości SzerokoscMM) (wiersz [CZ SK] [] 830))",
            Tree(Source));
    }

    [Fact]
    public void Context_has_attributes_and_a_file_of_values()
    {
        const string Source = """
            kontekst Kontrahent
              atrybut Asortyment : tekst
              atrybut Rynek : tekst
              wartości z "dane/kontrahenci.csv"
            """;

        Assert.Equal(
            "(kontekst Kontrahent (atrybut Asortyment tekst) (atrybut Rynek tekst) (wartości \"dane/kontrahenci.csv\"))",
            Tree(Source));
    }

    [Fact]
    public void File_keeps_its_declarations_in_order()
    {
        const string Source = """
            # Biblioteka cech.

            cecha Kierunek : wybór
              opcja L "Lewe"

            cecha SzerokoscMM : liczba
            wyliczana Szersze = SzerokoscMM - 800
            """;

        Assert.Equal(
            ["(cecha Kierunek wybór (opcja L \"Lewe\"))", "(cecha SzerokoscMM liczba)", "(wyliczana Szersze (- SzerokoscMM 800))"],
            Trees(Source));
    }
}
