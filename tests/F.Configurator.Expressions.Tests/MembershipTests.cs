namespace F.Configurator.Expressions.Tests;

// Grammar 6.1 level 5: `w` and `nie w`, against a closed range (`-5..-1`) or a list (`[CZ, SK]`).
public class MembershipTests
{
    [Fact]
    public void Number_inside_range_is_in_the_range()
    {
        var expression = Expression.In(Expression.Reference("Odchylka"), Expression.Range(-5m, -1m));
        var values = new Dictionary<string, Value> { ["Odchylka"] = Value.Number(-3m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // The range is closed on both ends.
    [Theory]
    [InlineData(-5, true)]
    [InlineData(-1, true)]
    [InlineData(-6, false)]
    [InlineData(0, false)]
    public void Range_includes_both_bounds_and_nothing_outside(int odchylka, bool expected)
    {
        var expression = Expression.In(Expression.Reference("Odchylka"), Expression.Range(-5m, -1m));
        var values = new Dictionary<string, Value> { ["Odchylka"] = Value.Number(odchylka) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(expected), value);
    }

    // Grammar 6.4: a missing value is not in anything.
    [Fact]
    public void Missing_value_is_not_in_a_range()
    {
        var expression = Expression.In(Expression.Reference("Odchylka"), Expression.Range(-5m, -1m));
        var values = new Dictionary<string, Value>();

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }

    [Fact]
    public void Number_outside_range_is_not_in_the_range()
    {
        var expression = Expression.NotIn(Expression.Reference("Odchylka"), Expression.Range(-5m, -1m));
        var values = new Dictionary<string, Value> { ["Odchylka"] = Value.Number(2m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Grammar 6.4: like `≠`, "A nie w X" is true when A has no value.
    [Fact]
    public void Missing_value_is_outside_every_range()
    {
        var expression = Expression.NotIn(Expression.Reference("Odchylka"), Expression.Range(-5m, -1m));
        var values = new Dictionary<string, Value>();

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }
}
