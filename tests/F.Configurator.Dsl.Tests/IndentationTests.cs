using static F.Configurator.Dsl.Tests.Lexing;

namespace F.Configurator.Dsl.Tests;

// Grammar 3.1: blocks come from indentation, as in Python. A deeper line opens a block, a shallower one closes every
// block down to its level.
//
// A long line can go on in the next line with deeper indentation (Basic: `dane A = 1,` / `B = 2`, `gdy A` / `i B`).
// The lexer joins such lines when the first one ends with a comma or an operator that needs a right side, or the next
// one starts with `i`, `lub`, `to`, `inaczej` or an arithmetic operator. Any other deeper line opens a block.
public class IndentationTests
{
    [Fact]
    public void Deeper_line_opens_a_block_that_closes_at_the_end()
    {
        Assert.Equal(
            [Keyword("reguła"), Name("R"), NewLine, Indent, Keyword("gdy"), Name("A"), NewLine, Dedent, End],
            Tokens("reguła R\n  gdy A\n"));
    }

    [Fact]
    public void Lines_of_one_block_have_the_same_indentation()
    {
        Assert.Equal(
            [Name("a"), NewLine, Indent, Name("b"), NewLine, Name("c"), NewLine, Dedent, Name("d"), NewLine, End],
            Tokens("a\n  b\n  c\nd"));
    }

    [Fact]
    public void Shallower_line_closes_every_deeper_block()
    {
        Assert.Equal(
            [Name("a"), NewLine, Indent, Name("b"), NewLine, Indent, Name("c"), NewLine, Dedent, Dedent, Name("d"),
                NewLine, End],
            Tokens("a\n  b\n    c\nd"));
    }

    [Fact]
    public void Shallower_line_closes_only_the_blocks_deeper_than_itself()
    {
        Assert.Equal(
            [Name("a"), NewLine, Indent, Name("b"), NewLine, Indent, Name("c"), NewLine, Dedent, Name("d"), NewLine,
                Dedent, End],
            Tokens("a\n  b\n    c\n  d"));
    }

    [Fact]
    public void Blank_and_comment_lines_do_not_close_a_block()
    {
        Assert.Equal(
            [Name("a"), NewLine, Indent, Name("b"), NewLine, Name("c"), NewLine, Dedent, End],
            Tokens("a\n  b\n\n# komentarz od początku wiersza\n  c"));
    }

    [Fact]
    public void Line_ending_with_a_comma_goes_on_in_the_next_line()
    {
        Assert.Equal(
            [Keyword("dane"), Name("A"), Operator("="), Number("1"), Operator(","), Name("B"), Operator("="),
                Number("2"), NewLine, End],
            Tokens("dane A = 1,\n     B = 2"));
    }

    [Theory]
    [InlineData("=")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("/")]
    [InlineData("i")]
    [InlineData("lub")]
    public void Line_ending_with_an_operator_goes_on_in_the_next_line(string @operator)
    {
        Assert.Equal(
            [Keyword("wyliczana"), Name("N"), OperatorOrKeyword(@operator), Name("B"), NewLine, End],
            Tokens($"wyliczana N {@operator}\n  B"));
    }

    [Theory]
    [InlineData("i")]
    [InlineData("lub")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("*")]
    [InlineData("/")]
    public void Line_starting_with_an_operator_goes_on_from_the_line_above(string @operator)
    {
        Assert.Equal(
            [Keyword("gdy"), Name("A"), OperatorOrKeyword(@operator), Name("B"), NewLine, End],
            Tokens($"gdy A\n  {@operator} B"));
    }

    // Basic: `wyliczana Nietyp =` / `jeśli …` / `to "T" inaczej "N"`.
    [Theory]
    [InlineData("to")]
    [InlineData("inaczej")]
    public void Line_starting_with_the_rest_of_a_condition_goes_on_from_the_line_above(string word)
    {
        Assert.Equal(
            [Keyword("jeśli"), Name("A"), Keyword(word), Name("B"), NewLine, End],
            Tokens($"jeśli A\n  {word} B"));
    }

    [Fact]
    public void Several_lines_go_on_one_after_another()
    {
        Assert.Equal(
            [Keyword("gdy"), Name("A"), Keyword("i"), Name("B"), Keyword("lub"), Name("C"), NewLine, End],
            Tokens("gdy A\n    i B\n    lub C"));
    }

    // The deeper indentation of a continued line is not a level, so the next line at the old level closes nothing.
    [Fact]
    public void Continued_line_does_not_open_a_block()
    {
        Assert.Equal(
            [Keyword("dane"), Name("A"), Operator(","), Name("B"), NewLine, Keyword("oczekuj"), Name("C"), NewLine,
                End],
            Tokens("dane A,\n     B\noczekuj C"));
    }

    [Fact]
    public void Block_can_follow_a_continued_line()
    {
        Assert.Equal(
            [Keyword("gdy"), Name("A"), Keyword("i"), Name("B"), NewLine, Indent, Name("C"), NewLine, Dedent, End],
            Tokens("gdy A\n    i B\n  C"));
    }

    [Fact]
    public void Line_that_is_not_deeper_does_not_go_on_even_after_a_comma()
    {
        Assert.Equal([Name("A"), Operator(","), NewLine, Name("B"), NewLine, End], Tokens("A,\nB"));
    }

    private static (TokenKind, string) OperatorOrKeyword(string written) =>
        char.IsLetter(written[0]) ? Keyword(written) : Operator(written);
}
