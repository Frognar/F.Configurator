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

    // Grammar 6.4: a condition with a missing value is false, so `inaczej` applies.
    [Theory]
    [InlineData(900)]
    [InlineData(null)]
    public void False_or_missing_condition_evaluates_to_the_otherwise_branch(int? szerokosc)
    {
        var expression = Expression.If(
            Expression.GreaterThan(Expression.Reference("SzerokoscMM"), Expression.Number(1000m)),
            Expression.Number(70m),
            Expression.Number(0m));
        var values = new Dictionary<string, Value>();
        if (szerokosc is { } amount)
        {
            values["SzerokoscMM"] = Value.Number(amount);
        }

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(0m), value);
    }
}
