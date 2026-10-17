using static F.Configurator.Dsl.Tests.Declarations;

namespace F.Configurator.Dsl.Tests;

// Grammar 4.11: a test of the package names its collection, sets values after `dane` in the order a user would, and
// checks the result after `oczekuj`.
public class PackageTestParserTests
{
    private static string Expect(string expectations) => Tree($"test \"T\"\n  oczekuj {expectations}");

    [Fact]
    public void Test_names_its_collection_and_data()
    {
        const string Source = """
            test "Standard PL 80"
              kolekcja Demo
              dane Kontrahent = "13-WEBICO", Model = STANDARD_01, Kontrahent.Asortyment = STANDARD
            """;

        Assert.Equal(
            "(test \"Standard PL 80\" (kolekcja Demo) "
            + "(dane (= Kontrahent \"13-WEBICO\") (= Model STANDARD_01) (= (. Kontrahent Asortyment) STANDARD)))",
            Tree(Source));
    }

    [Fact]
    public void Expectation_checks_values()
    {
        Assert.Equal(
            "(test \"T\" (oczekuj (= SzerokoscMM 844) (= Nietyp \"N\")))",
            Expect("SzerokoscMM = 844, Nietyp = \"N\""));
    }

    [Theory]
    [InlineData("indeks zawiera \"TTT\"", "(indeks zawiera \"TTT\")")]
    [InlineData("indeks = \"S1PL80N8442030\"", "(indeks \"S1PL80N8442030\")")]
    public void Expectation_checks_the_index(string expectation, string tree)
    {
        Assert.Equal($"(test \"T\" (oczekuj {tree}))", Expect(expectation));
    }

    [Theory]
    [InlineData("niedostępne Kierunek [L, P]", "(niedostępne Kierunek [L P])")]
    [InlineData("ukryta ZmianaPolozeniaZamka", "(ukryta ZmianaPolozeniaZamka)")]
    [InlineData("komunikat MaxSzer", "(komunikat MaxSzer)")]
    public void Expectation_checks_what_the_wizard_shows(string expectation, string tree)
    {
        Assert.Equal($"(test \"T\" (oczekuj {tree}))", Expect(expectation));
    }

    [Theory]
    [InlineData("cena = 850.00", "(cena 850.00)")]
    [InlineData("składnik WezszeDo5 = 70", "(składnik WezszeDo5 70)")]
    public void Expectation_checks_the_price(string expectation, string tree)
    {
        Assert.Equal($"(test \"T\" (oczekuj {tree}))", Expect(expectation));
    }

    [Fact]
    public void Lines_of_data_and_expectations_keep_their_order()
    {
        const string Source = """
            test "Za szeroko"
              kolekcja Demo
              dane Model = STANDARD_01,
                   Norma = PL
              dane SzerokoscMM = 950
              oczekuj SzerokoscMM = 894, komunikat MaxSzer
              oczekuj cena = 695
            """;

        Assert.Equal(
            "(test \"Za szeroko\" (kolekcja Demo) (dane (= Model STANDARD_01) (= Norma PL) (= SzerokoscMM 950)) "
            + "(oczekuj (= SzerokoscMM 894) (komunikat MaxSzer) (cena 695)))",
            Tree(Source));
    }
}
