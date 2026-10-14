using static F.Configurator.Dsl.Tests.Lexing;

namespace F.Configurator.Dsl.Tests;

// Grammar 3.2–3.4: names, values, keywords and operators. Each line ends with a new line token and the source with
// the end token, so the parser never has to check for running out of tokens.
public class TokenTests
{
    [Fact]
    public void Empty_source_is_only_the_end()
    {
        Assert.Equal([End], Tokens(""));
    }

    [Fact]
    public void Name_is_a_token()
    {
        Assert.Equal([Name("SzerokoscMM"), NewLine, End], Tokens("SzerokoscMM"));
    }

    [Fact]
    public void Name_may_have_polish_letters_digits_and_underscores()
    {
        Assert.Equal([Name("SzerokośćMM_2"), NewLine, End], Tokens("SzerokośćMM_2"));
    }

    [Fact]
    public void Spaces_separate_tokens()
    {
        Assert.Equal([Name("Model"), Name("Seria"), NewLine, End], Tokens("Model   Seria  "));
    }

    [Fact]
    public void Lines_are_separated_by_new_lines()
    {
        Assert.Equal([Name("A"), NewLine, Name("B"), NewLine, End], Tokens("A\nB\n"));
    }

    [Fact]
    public void Windows_line_ends_are_new_lines_too()
    {
        Assert.Equal([Name("A"), NewLine, Name("B"), NewLine, End], Tokens("A\r\nB\r\n"));
    }

    [Fact]
    public void Comment_lasts_to_the_end_of_the_line()
    {
        Assert.Equal([Name("Model"), NewLine, End], Tokens("Model # wybór modelu"));
    }

    [Fact]
    public void Blank_lines_and_comment_lines_give_no_tokens()
    {
        Assert.Equal([Name("A"), NewLine, Name("B"), NewLine, End], Tokens("\n# nagłówek\nA\n\n   \n  # wcięty komentarz\nB"));
    }

    [Fact]
    public void Keyword_is_a_token_of_its_own()
    {
        Assert.Equal([Keyword("reguła"), Name("Wymiary"), NewLine, End], Tokens("reguła Wymiary"));
    }

    // Grammar 3.4: every keyword written with Polish letters has an equivalent without them. The token always carries
    // the form with Polish letters, so the parser knows one spelling.
    [Theory]
    [InlineData("regula", "reguła")]
    [InlineData("wybor", "wybór")]
    [InlineData("blad", "błąd")]
    [InlineData("jesli", "jeśli")]
    [InlineData("domyslnie", "domyślnie")]
    [InlineData("dlugosc", "długość")]
    [InlineData("zabron", "zabroń")]
    [InlineData("wartosci", "wartości")]
    [InlineData("ostrzez", "ostrzeż")]
    [InlineData("niezalezny", "niezależny")]
    [InlineData("kontrahentow", "kontrahentów")]
    public void Keyword_without_polish_letters_is_the_same_keyword(string written, string keyword)
    {
        Assert.Equal([Keyword(keyword), NewLine, End], Tokens(written));
    }

    // Only the two whole spellings are keywords; anything in between is an ordinary name.
    [Fact]
    public void Keyword_with_some_polish_letters_left_out_is_a_name()
    {
        Assert.Equal([Name("dlugość"), NewLine, End], Tokens("dlugość"));
    }

    [Theory]
    [InlineData("Reguła")]
    [InlineData("TAK")]
    [InlineData("NIE")]
    [InlineData("Gdy")]
    public void Keywords_are_lower_case_so_other_spellings_are_names(string written)
    {
        Assert.Equal([Name(written), NewLine, End], Tokens(written));
    }

    public static TheoryData<string> AllKeywords() =>
    [
        // declarations
        "katalog", "cecha", "opcja", "atrybut", "wyliczana", "tabela", "kolekcja", "etap", "komponent", "kontekst",
        "reguła", "powiązanie", "indeks", "wizualizacja", "warstwa", "test", "cennik", "dopłata", "przypisz",
        // properties
        "nazwa", "opis", "prezentacja", "jednostka", "wymagana", "opcje", "atrybuty", "symbol", "obraz", "z", "klucz",
        "wartości", "domyślnie", "min", "maks", "krok", "długość", "znaki", "wzorzec", "reakcja", "tryb", "cechy",
        "wspólne", "własne", "priorytet", "dotyczy", "gdy", "wtedy", "przy", "naruszeniu",
        // effects
        "ukryj", "zablokuj", "tylko", "zabroń", "ustaw", "koryguj", "błąd", "ostrzeż", "informuj",
        // expressions
        "i", "lub", "nie", "w", "jeśli", "to", "inaczej",
        // related items
        "do", "proponuj", "przejmij", "jako",
        // price lists
        "waluta", "baza", "kwota", "rozszerza", "wyłącz", "nadpisz", "rabat", "narzut", "zaokrąglij", "kontrahenta",
        "kontrahentów",
        // tests
        "dane", "oczekuj", "zawiera", "niedostępne", "komunikat",
        // feature types, words of the EBNF and G15/G18
        "wybór", "wielokrotny", "liczba", "tekst", "tak/nie", "kolejny", "niezależny", "widoczna", "wszystkich",
        "domyślne", "zablokowane", "domyślny", "dozwolone", "tekstura", "odbij", "cena", "składnik", "ukryta",
        // built-in functions, so `domyslna(…)` means `domyślna(…)` as keywords do
        "domyślna", "wybrana", "dopełnij",
    ];

    [Theory]
    [MemberData(nameof(AllKeywords))]
    public void Every_word_of_the_grammar_is_a_keyword(string keyword)
    {
        Assert.Equal([Keyword(keyword), NewLine, End], Tokens(keyword));
    }

    [Fact]
    public void Yes_no_type_is_one_keyword()
    {
        Assert.Equal(
            [Keyword("cecha"), Name("KrawedzBPGorna"), Operator(":"), Keyword("tak/nie"), NewLine, End],
            Tokens("cecha KrawedzBPGorna : tak/nie"));
    }

    [Theory]
    [InlineData("1044")]
    [InlineData("0.5")]
    [InlineData("007")]
    public void Number_keeps_its_digits_and_decimal_point(string written)
    {
        Assert.Equal([Number(written), NewLine, End], Tokens(written));
    }

    [Fact]
    public void Minus_before_a_number_is_an_operator()
    {
        Assert.Equal([Operator("-"), Number("5"), NewLine, End], Tokens("-5"));
    }

    [Fact]
    public void Range_is_two_numbers_around_two_dots()
    {
        Assert.Equal(
            [Operator("-"), Number("5"), Operator(".."), Operator("-"), Number("1"), NewLine, End],
            Tokens("-5..-1"));
    }

    [Fact]
    public void Percent_is_a_number_and_a_percent_sign()
    {
        Assert.Equal([Number("5"), Operator("%"), NewLine, End], Tokens("5%"));
    }

    // Grammar 3.2 and 3.3: a quoted option id and a text are written the same way; the parser tells them apart.
    [Fact]
    public void Text_is_the_content_between_quotes()
    {
        Assert.Equal([Text("Wysokość poniżej 1970 mm"), NewLine, End], Tokens("\"Wysokość poniżej 1970 mm\""));
    }

    [Fact]
    public void Text_may_hold_an_escaped_quote()
    {
        Assert.Equal([Text("drzwi \"Lux\""), NewLine, End], Tokens("\"drzwi \\\"Lux\\\"\""));
    }

    [Fact]
    public void Text_keeps_placeholders_and_hashes()
    {
        Assert.Equal([Text("Maksymalnie {maks} mm # 2"), NewLine, End], Tokens("\"Maksymalnie {maks} mm # 2\""));
    }

    [Fact]
    public void Empty_text_is_a_text()
    {
        Assert.Equal([Text(""), NewLine, End], Tokens("\"\""));
    }

    [Theory]
    [InlineData("=")]
    [InlineData("≠")]
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData(">")]
    [InlineData(">=")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("/")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("[")]
    [InlineData("]")]
    [InlineData(",")]
    [InlineData(":")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("|")]
    [InlineData("%")]
    public void Operator_is_a_token(string written)
    {
        Assert.Equal([Operator(written), NewLine, End], Tokens(written));
    }

    // Grammar 6.1: `!=` is another way to write `≠`, and `≤`, `≥` are taken as `<=`, `>=`.
    [Theory]
    [InlineData("!=", "≠")]
    [InlineData("≤", "<=")]
    [InlineData("≥", ">=")]
    public void Operator_written_another_way_is_the_same_operator(string written, string @operator)
    {
        Assert.Equal([Operator(@operator), NewLine, End], Tokens(written));
    }

    [Fact]
    public void Operators_need_no_spaces_around_them()
    {
        Assert.Equal(
            [Name("A"), Operator("<="), Number("5"), Operator("*"), Operator("("), Name("B"), Operator("-"), Number("1"),
                Operator(")"), NewLine, End],
            Tokens("A<=5*(B-1)"));
    }

    [Fact]
    public void Reading_from_a_table_is_names_and_operators()
    {
        Assert.Equal(
            [Name("Wymiary"), Operator("["), Name("Norma"), Operator(","), Name("Szerokosc"), Operator("]"),
                Operator("."), Name("SzerokoscMM"), NewLine, End],
            Tokens("Wymiary[Norma, Szerokosc].SzerokoscMM"));
    }

    [Fact]
    public void Function_call_is_a_keyword_and_brackets()
    {
        Assert.Equal(
            [Keyword("domyślna"), Operator("("), Name("SzerokoscMM"), Operator(")"), Operator("-"), Number("100"),
                NewLine, End],
            Tokens("domyslna(SzerokoscMM) - 100"));
    }
}
