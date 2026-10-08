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

    // Grammar 6.4: "A is not X" is true when A has no value.
    [Fact]
    public void Missing_value_is_not_equal_to_a_number()
    {
        var expression = Expression.NotEqual(Expression.Reference("SzerokoscMM"), Expression.Number(900m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Boolean(true), value);
    }

    [Fact]
    public void Equal_numbers_are_not_unequal()
    {
        var expression = Expression.NotEqual(Expression.Reference("SzerokoscMM"), Expression.Number(900m));
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(900m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }

    [Fact]
    public void Smaller_number_is_less_than_larger_number()
    {
        var expression = Expression.LessThan(Expression.Reference("SzerokoscMM"), Expression.Number(900m));
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(800m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Grammar 6.4: a comparison with a missing value is false, it does not propagate the missing value.
    [Fact]
    public void Missing_value_is_not_less_than_a_number()
    {
        var expression = Expression.LessThan(Expression.Reference("SzerokoscMM"), Expression.Number(900m));
        var values = new Dictionary<string, Value>();

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }

    [Fact]
    public void Equal_number_is_less_than_or_equal()
    {
        var expression = Expression.LessThanOrEqual(Expression.Reference("SzerokoscMM"), Expression.Number(900m));
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(900m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    [Fact]
    public void Larger_number_is_greater_than_smaller_number()
    {
        var expression = Expression.GreaterThan(Expression.Reference("WysokoscMM"), Expression.Number(2000m));
        var values = new Dictionary<string, Value> { ["WysokoscMM"] = Value.Number(2100m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    [Fact]
    public void Equal_number_is_greater_than_or_equal()
    {
        var expression = Expression.GreaterThanOrEqual(Expression.Reference("WysokoscMM"), Expression.Number(2000m));
        var values = new Dictionary<string, Value> { ["WysokoscMM"] = Value.Number(2000m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Grammar 4.2: a choice feature's value is one of its options, written by id (`Kraj = PL`).
    [Fact]
    public void Equal_options_compare_as_equal()
    {
        var expression = Expression.Equal(Expression.Reference("Kraj"), Expression.Option("PL"));
        var values = new Dictionary<string, Value> { ["Kraj"] = Value.Option("PL") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // An option and a text with the same spelling are different values: `Kraj = PL` is not `Kraj = "PL"`.
    [Fact]
    public void Option_is_not_equal_to_text_with_the_same_spelling()
    {
        var expression = Expression.Equal(Expression.Option("PL"), Expression.Text("PL"));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Boolean(false), value);
    }
}
