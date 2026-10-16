using F.Configurator.Dsl.Syntax;
using static F.Configurator.Dsl.Tests.Parsing;

namespace F.Configurator.Dsl.Tests;

// Each node knows the text it was read from, so later messages (unknown name, wrong type) can underline it. A node
// on one line covers it from its first to its last character.
public class ExpressionSpanTests
{
    private static ExpressionSyntax Syntax(string source) => ParseExpression(source).Syntax!;

    [Fact]
    public void Name_covers_its_token()
    {
        Assert.Equal(At(1, 1, 8), Syntax("Wysokość").Span);
    }

    [Fact]
    public void Operation_covers_both_sides()
    {
        var sum = (BinarySyntax)Syntax("A + 10");

        Assert.Equal(At(1, 1, 6), sum.Span);
        Assert.Equal(At(1, 5, 2), sum.Right.Span);
    }

    [Fact]
    public void Function_covers_its_closing_parenthesis()
    {
        Assert.Equal(At(1, 1, 10), Syntax("wybrana(X)").Span);
    }

    [Fact]
    public void Group_covers_its_parentheses()
    {
        var product = (BinarySyntax)Syntax("(A + B) * 2");

        Assert.Equal(At(1, 1, 7), product.Left.Span);
    }

    [Fact]
    public void Member_covers_the_table_row_and_column()
    {
        Assert.Equal(At(1, 1, 18), Syntax("Tab[Norma].Kolumna").Span);
    }

    [Fact]
    public void Text_covers_its_quotes()
    {
        Assert.Equal(At(1, 5, 6), ((BinarySyntax)Syntax("A = \"26MM\"")).Right.Span);
    }

    [Fact]
    public void Condition_covers_everything_up_to_its_last_value()
    {
        Assert.Equal(At(1, 1, 26), Syntax("jeśli A to 1 inaczej 2 + 3").Span);
    }
}
