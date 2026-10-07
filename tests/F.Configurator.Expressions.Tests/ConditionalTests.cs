namespace F.Configurator.Expressions.Tests;

// Grammar 6.1 level 1: `jeśli … to … inaczej …`.
public class ConditionalTests
{
    [Fact]
    public void True_condition_evaluates_to_the_then_branch()
    {
        var expression = Expression.If(
            Expression.GreaterThan(Expression.Reference("SzerokoscMM"), Expression.Number(1000m)),
            Expression.Number(70m),
            Expression.Number(0m));
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(1100m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(70m), value);
    }
}
