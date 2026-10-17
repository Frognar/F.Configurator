using static F.Configurator.Dsl.Tests.Declarations;

namespace F.Configurator.Dsl.Tests;

// Grammar 5: a price list gives the base price and the surcharges, may extend another one, and is assigned to
// customers. Its other lines (`waluta`, `opis`) are properties, checked later.
public class PriceListParserTests
{
    private static string Line(string line) => Tree($"cennik C\n  {line}");

    [Fact]
    public void Price_list_has_a_name_and_properties()
    {
        const string Source = """
            cennik Domyslny
              waluta PLN
            """;

        Assert.Equal("(cennik Domyslny (waluta PLN))", Tree(Source));
    }

    [Theory]
    [InlineData("cennik Domyslny domyślny", "(cennik Domyslny domyślny)")]
    [InlineData("cennik Promocja rozszerza Domyslny", "(cennik Promocja (rozszerza Domyslny))")]
    [InlineData("cennik Nowy rozszerza Stary domyślny", "(cennik Nowy (rozszerza Stary) domyślny)")]
    public void Price_list_may_extend_another_and_be_the_default(string source, string tree)
    {
        Assert.Equal(tree, Tree(source));
    }

    [Fact]
    public void Base_price_comes_from_a_file_by_its_keys()
    {
        Assert.Equal(
            "(cennik C (cena bazowa \"dane/ceny.csv\" (klucz Model (. Kolor GrupaCenowa))))",
            Line("baza z \"dane/ceny.csv\" klucz Model, Kolor.GrupaCenowa"));
    }

    [Fact]
    public void Base_price_may_be_written_in_place()
    {
        const string Source = """
            cennik C
              baza
                klucz Model
                | STANDARD_01  | 600 |
                | ASTORIA_GOLD | 950 |
            """;

        Assert.Equal(
            "(cennik C (cena bazowa (klucz Model) (wiersz STANDARD_01 600) (wiersz ASTORIA_GOLD 950)))",
            Tree(Source));
    }

    [Fact]
    public void Key_of_the_base_price_file_may_be_written_below()
    {
        const string Source = """
            cennik C
              baza z "dane/ceny.csv"
                klucz Model, Typ
            """;

        Assert.Equal("(cennik C (cena bazowa \"dane/ceny.csv\" (klucz Model Typ)))", Tree(Source));
    }

    [Fact]
    public void Option_surcharges_come_from_a_file()
    {
        Assert.Equal("(cennik C (opcje z \"dane/doplaty-opcji.csv\"))", Line("opcje z \"dane/doplaty-opcji.csv\""));
    }

    [Fact]
    public void Surcharge_has_an_amount_and_may_have_a_condition()
    {
        const string Source = """
            cennik C
              dopłata W5 "węższe do 5 mm"
                gdy Norma w [CZ, SK] i OdchylkaSzerokosc w -5..-1
                kwota 70
              dopłata Przesuwne "drzwi przesuwne"
                kwota Przesuwne[FelcSkrzydla, Szerokosc].Kwota
            """;

        Assert.Equal(
            "(cennik C (dopłata W5 \"węższe do 5 mm\" (warunek (i (w Norma [CZ SK]) (w OdchylkaSzerokosc (.. (- 5) (- 1))))) "
            + "(kwota 70)) (dopłata Przesuwne \"drzwi przesuwne\" (kwota (. ([] Przesuwne FelcSkrzydla Szerokosc) Kwota))))",
            Tree(Source));
    }

    [Fact]
    public void Price_list_may_leave_out_or_change_surcharges_of_the_one_it_extends()
    {
        const string Source = """
            cennik Promocja rozszerza Domyslny
              wyłącz W5, W5P
              nadpisz Ramiak kwota 30
            """;

        Assert.Equal(
            "(cennik Promocja (rozszerza Domyslny) (wyłączone W5 W5P) (nadpisz Ramiak (kwota 30)))",
            Tree(Source));
    }

    // Grammar G8: the only place where the order of lines matters.
    [Fact]
    public void Discounts_markups_and_rounding_keep_their_order()
    {
        const string Source = """
            cennik C
              narzut 2.5%
              zaokrąglij 1
              rabat 5%
            """;

        Assert.Equal("(cennik C (modyfikatory (narzut 2.5%) (zaokrąglij 1) (rabat 5%)))", Tree(Source));
    }

    [Fact]
    public void Price_list_holds_its_own_tables()
    {
        const string Source = """
            cennik C
              tabela Przejscie z "dane/przejscia.csv"
                klucz Rozmiar, Grupa
                wartości Kwota
            """;

        Assert.Equal(
            "(cennik C (tabela Przejscie \"dane/przejscia.csv\" (klucz Rozmiar Grupa) (wartości Kwota)))",
            Tree(Source));
    }

    [Theory]
    [InlineData("do kontrahenta \"13-DOORS4U\"", "(kontrahenci \"13-DOORS4U\")")]
    [InlineData("do kontrahentów [\"13-WEBICO\", \"13-ASARK\"]", "(kontrahenci \"13-WEBICO\" \"13-ASARK\")")]
    [InlineData("do kontrahentów \"13-WEBICO\", \"13-ASARK\"", "(kontrahenci \"13-WEBICO\" \"13-ASARK\")")]
    [InlineData("do kontrahentów z \"dane/cenniki-kontrahentow.csv\"", "(kontrahenci z \"dane/cenniki-kontrahentow.csv\")")]
    public void Price_list_is_assigned_to_customers(string customers, string tree)
    {
        Assert.Equal($"(przypisz Promocja {tree})", Tree($"przypisz Promocja {customers}"));
    }

    [Fact]
    public void List_of_customers_may_go_on_in_the_next_line()
    {
        const string Source = """
            przypisz Promocja do kontrahentów "13-WEBICO",
              "13-ASARK"
            """;

        Assert.Equal("(przypisz Promocja (kontrahenci \"13-WEBICO\" \"13-ASARK\"))", Tree(Source));
    }
}
