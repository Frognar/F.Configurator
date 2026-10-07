namespace F.Configurator.Expressions.Tests;

public class LiteralTests
{
    [Fact]
    public void Number_literal_evaluates_to_itself()
    {
        var expression = Expression.Number(1044m);

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Number(1044m), value);
    }
}
