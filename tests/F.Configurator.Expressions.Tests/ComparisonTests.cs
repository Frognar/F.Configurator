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
}
