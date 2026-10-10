namespace F.Configurator.Expressions.Tests;

public class LiteralTests
{
    [Fact]
    public void Number_literal_evaluates_to_itself()
    {
        var expression = Number(1044m);

        var value = expression.Evaluate(NoValues);

        Assert.Equal(Value.Number(1044m), value);
    }

    [Fact]
    public void Text_literal_evaluates_to_itself()
    {
        var expression = Text("ASTORIA");

        var value = expression.Evaluate(NoValues);

        Assert.Equal(Value.Text("ASTORIA"), value);
    }
}
