using static F.Configurator.Dsl.Tests.Declarations;

namespace F.Configurator.Dsl.Tests;

// Grammar 4.7: a rule is `reguła`, an optional condition after `gdy`, effects after `wtedy` and an optional reaction
// to a value it does not allow. Its other lines (`opis`, `dotyczy`, `priorytet`) are properties, checked later.
public class RuleParserTests
{
    private static string Effect(string effect) => Tree($"reguła R\n  wtedy {effect}");

    [Fact]
    public void Rule_has_a_name_and_an_effect()
    {
        const string Source = """
            reguła FelcXWymiary
              wtedy zablokuj SzerokoscMM, WysokoscMM
            """;

        Assert.Equal("(reguła FelcXWymiary (zablokuj SzerokoscMM WysokoscMM))", Tree(Source));
    }

    [Fact]
    public void Rule_may_have_a_condition()
    {
        const string Source = """
            reguła FelcXWymiary
              gdy FelcSkrzydla = X
              wtedy zablokuj SzerokoscMM
            """;

        Assert.Equal("(reguła FelcXWymiary (warunek (= FelcSkrzydla X)) (zablokuj SzerokoscMM))", Tree(Source));
    }

    [Fact]
    public void Rule_lists_its_properties()
    {
        const string Source = """
            reguła MaxSzerBasic
              opis "Limit szerokości"
              dotyczy Brilliant, Basic
              priorytet 10
              wtedy zablokuj SzerokoscMM
            """;

        Assert.Equal(
            "(reguła MaxSzerBasic (opis \"Limit szerokości\") (dotyczy Brilliant Basic) (priorytet 10) (zablokuj SzerokoscMM))",
            Tree(Source));
    }

    [Fact]
    public void Rule_may_apply_to_every_collection()
    {
        const string Source = """
            reguła R
              dotyczy wszystkich
              wtedy ukryj A
            """;

        Assert.Equal("(reguła R (dotyczy wszystkich) (ukryj A))", Tree(Source));
    }

    [Fact]
    public void Effects_may_follow_in_deeper_lines()
    {
        const string Source = """
            reguła Domyslne
              wtedy ukryj A
                    zablokuj B
                    ukryj C
            """;

        Assert.Equal("(reguła Domyslne (ukryj A) (zablokuj B) (ukryj C))", Tree(Source));
    }

    [Theory]
    [InlineData("ukryj A, B", "(ukryj A B)")]
    [InlineData("zablokuj Bierne.SzerokoscMM", "(zablokuj (. Bierne SzerokoscMM))")]
    public void Effect_hides_or_locks_features(string effect, string tree)
    {
        Assert.Equal($"(reguła R {tree})", Effect(effect));
    }

    [Theory]
    [InlineData("tylko Norma [PL, CZ]", "(tylko Norma [PL CZ])")]
    [InlineData("zabroń Bierne.ZamekDolny [KLUCZ]", "(zabroń (. Bierne ZamekDolny) [KLUCZ])")]
    [InlineData("tylko Model z DozwoloneModel", "(tylko Model z DozwoloneModel)")]
    public void Effect_allows_or_forbids_options(string effect, string tree)
    {
        Assert.Equal($"(reguła R {tree})", Effect(effect));
    }

    [Theory]
    [InlineData("min SzerokoscMM = 600", "(min SzerokoscMM 600)")]
    [InlineData("maks ZmianaPolozenia = WysokoscMM / 10", "(maks ZmianaPolozenia (/ WysokoscMM 10))")]
    [InlineData("krok SzerokoscMM = 5", "(krok SzerokoscMM 5)")]
    [InlineData("długość Grawer = 0..12", "(długość Grawer (.. 0 12))")]
    [InlineData("znaki Grawer = cyfry", "(znaki Grawer cyfry)")]
    public void Effect_limits_a_value(string effect, string tree)
    {
        Assert.Equal($"(reguła R {tree})", Effect(effect));
    }

    [Theory]
    [InlineData("domyślnie Norma = PL", "(domyślnie Norma PL)")]
    [InlineData("domyślnie SzerokoscMM, WysokoscMM z WymiaryDomyslne", "(domyślnie SzerokoscMM WysokoscMM z WymiaryDomyslne)")]
    [InlineData("ustaw Podciecie = TAK", "(ustaw Podciecie TAK)")]
    public void Effect_gives_a_value(string effect, string tree)
    {
        Assert.Equal($"(reguła R {tree})", Effect(effect));
    }

    [Theory]
    [InlineData("błąd \"Brak wymiarów dla {Norma}.\"", "(błąd \"Brak wymiarów dla {Norma}.\")")]
    [InlineData("ostrzeż \"Wysokość poniżej 1970 mm\"", "(ostrzeż \"Wysokość poniżej 1970 mm\")")]
    [InlineData("informuj \"Zmieniono kolor\"", "(informuj \"Zmieniono kolor\")")]
    public void Effect_shows_a_message(string effect, string tree)
    {
        Assert.Equal($"(reguła R {tree})", Effect(effect));
    }

    [Fact]
    public void Rule_may_react_to_a_value_it_does_not_allow()
    {
        const string Source = """
            reguła MaxSzer
              wtedy maks SzerokoscMM = 1044
              przy naruszeniu koryguj "Maksymalnie {maks} mm."
            """;

        Assert.Equal("(reguła MaxSzer (maks SzerokoscMM 1044) (przy naruszeniu koryguj \"Maksymalnie {maks} mm.\"))", Tree(Source));
    }
}
