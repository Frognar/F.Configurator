using static F.Configurator.Dsl.Tests.Lexing;

namespace F.Configurator.Dsl.Tests;

// Grammar 7: a problem has a place and a message in Polish. The lexer reports it and goes on, so one run shows every
// problem of the file.
public class LexerDiagnosticsTests
{
    [Fact]
    public void Correct_source_has_no_diagnostics()
    {
        Assert.Empty(Lex("reguła R\n  gdy A ≠ \"x\"\n").Diagnostics);
    }

    [Fact]
    public void Tab_in_indentation_is_an_error()
    {
        Assert.Equal(
            [new Diagnostic(Severity.Error, At(2, 1, 1), "tabulator we wcięciu, użyj spacji", null)],
            Lex("reguła R\n\tgdy A").Diagnostics);
    }

    [Fact]
    public void Indentation_between_two_levels_is_an_error()
    {
        Assert.Equal(
            [new Diagnostic(Severity.Error, At(3, 1, 2), "wcięcie nie pasuje do żadnego z poprzednich poziomów", null)],
            Lex("a\n    b\n  c").Diagnostics);
    }

    [Fact]
    public void Text_without_closing_quote_is_an_error_up_to_the_end_of_the_line()
    {
        Assert.Equal(
            [new Diagnostic(Severity.Error, At(1, 5, 4), "tekst bez cudzysłowu zamykającego", null)],
            Lex("A = \"abc\nB").Diagnostics);
    }

    [Fact]
    public void Text_without_closing_quote_ends_with_its_line()
    {
        Assert.Equal(
            [Name("A"), Operator("="), Text("abc"), NewLine, Name("B"), NewLine, End],
            Tokens("A = \"abc\nB"));
    }

    [Fact]
    public void Unknown_character_is_an_error()
    {
        Assert.Equal(
            [new Diagnostic(Severity.Error, At(1, 3, 1), "nieznany znak „@”", null)],
            Lex("A @ B").Diagnostics);
    }

    [Fact]
    public void Unknown_character_is_skipped()
    {
        Assert.Equal([Name("A"), Name("B"), NewLine, End], Tokens("A @ B"));
    }

    [Fact]
    public void Exclamation_mark_alone_is_an_unknown_character()
    {
        Assert.Equal(
            [new Diagnostic(Severity.Error, At(1, 3, 1), "nieznany znak „!”", null)],
            Lex("A ! B").Diagnostics);
    }

    // Grammar 7: the decimal separator is a dot; a comma separates list items, so `0,5` is two numbers.
    [Fact]
    public void Decimal_comma_is_an_error_with_a_hint()
    {
        Assert.Equal(
            [new Diagnostic(Severity.Error, At(1, 8, 3), "liczba „0,5” – użyj kropki", "0.5")],
            Lex("krok = 0,5").Diagnostics);
    }

    [Fact]
    public void Comma_followed_by_a_space_separates_numbers()
    {
        Assert.Empty(Lex("[60, 70]").Diagnostics);
    }
}
