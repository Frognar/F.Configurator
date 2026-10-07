namespace F.Configurator.Expressions.Tests;

public class ComparisonTests
{
    [Fact]
    public void Equal_numbers_compare_as_equal()
    {
        var expression = Expression.Equal(Expression.Reference("SzerokoscMM"), Expression.Number(900m));
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(900m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Grammar 6.4: a missing value is not equal to anything, not even another missing value.
    [Fact]
    public void Missing_values_do_not_compare_as_equal()
    {
        var expression = Expression.Equal(Expression.Reference("SzerokoscMM"), Expression.Reference("WysokoscMM"));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Boolean(false), value);
    }
}
