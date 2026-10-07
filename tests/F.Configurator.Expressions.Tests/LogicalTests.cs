namespace F.Configurator.Expressions.Tests;

// Grammar 6.1 levels 2-4: `lub`, `i`, `nie`.
public class LogicalTests
{
    [Fact]
    public void And_of_two_true_conditions_is_true()
    {
        var expression = Expression.And(
            Expression.GreaterThan(Expression.Reference("SzerokoscMM"), Expression.Number(800m)),
            Expression.LessThan(Expression.Reference("WysokoscMM"), Expression.Number(2100m)));
        var values = new Dictionary<string, Value>
        {
            ["SzerokoscMM"] = Value.Number(900m),
            ["WysokoscMM"] = Value.Number(2000m),
        };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }
}
