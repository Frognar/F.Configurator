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

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void And_with_a_false_condition_is_false(bool left, bool right)
    {
        var expression = Expression.And(Expression.Reference("A"), Expression.Reference("B"));
        var values = new Dictionary<string, Value> { ["A"] = Value.Boolean(left), ["B"] = Value.Boolean(right) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }

    // Grammar 6.4: a condition with a missing value is false.
    [Fact]
    public void And_with_a_missing_condition_is_false()
    {
        var expression = Expression.And(Expression.Reference("A"), Expression.Reference("B"));
        var values = new Dictionary<string, Value> { ["B"] = Value.Boolean(true) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }

    [Fact]
    public void Or_with_one_true_condition_is_true()
    {
        var expression = Expression.Or(Expression.Reference("A"), Expression.Reference("B"));
        var values = new Dictionary<string, Value> { ["A"] = Value.Boolean(false), ["B"] = Value.Boolean(true) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Grammar 6.4: a missing condition counts as false, so only a true one makes `lub` true.
    [Fact]
    public void Or_without_a_true_condition_is_false()
    {
        var expression = Expression.Or(Expression.Reference("A"), Expression.Reference("B"));
        var values = new Dictionary<string, Value> { ["B"] = Value.Boolean(false) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }
}
