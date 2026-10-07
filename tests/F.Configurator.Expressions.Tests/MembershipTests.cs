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
}
