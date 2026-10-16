using static F.Configurator.Dsl.Tests.Lexing;

namespace F.Configurator.Dsl.Tests;

// Grammar 7: every message points at a file, line and column (counted from 1) and marks the characters it is about.
public class SpanTests
{
    [Fact]
    public void Token_knows_where_it_was_written()
    {
        var tokens = Lex("reguła Wysokość\n  gdy A ≠ 5").Tokens;

        Assert.Equal(
            [
                new Token(TokenKind.Keyword, "reguła", At(1, 1, 6)),
                new Token(TokenKind.Name, "Wysokość", At(1, 8, 8)),
                new Token(TokenKind.Keyword, "gdy", At(2, 3, 3)),
                new Token(TokenKind.Name, "A", At(2, 7, 1)),
                new Token(TokenKind.Operator, "≠", At(2, 9, 1)),
                new Token(TokenKind.Number, "5", At(2, 11, 1)),
            ],
            tokens.Where(t => t.Kind is not (TokenKind.NewLine or TokenKind.Indent or TokenKind.Dedent or TokenKind.End)));
    }

    [Fact]
    public void Text_span_covers_its_quotes_and_escapes()
    {
        Assert.Equal(At(1, 3, 7), Lex("= \"a\\\"bc\"").Tokens[1].Span);
    }

    [Fact]
    public void Span_covers_what_was_written_not_the_keyword_it_means()
    {
        Assert.Equal(At(1, 1, 9), Lex("domyslnie").Tokens[0].Span);
    }

    [Fact]
    public void Span_covers_an_operator_written_another_way()
    {
        Assert.Equal(At(1, 3, 2), Lex("A != B").Tokens[1].Span);
    }

    [Fact]
    public void Span_of_a_continued_line_is_in_that_line()
    {
        Assert.Equal(At(2, 5, 1), Lex("A +\n    B").Tokens[2].Span);
    }
}
