namespace F.Configurator.Expressions.Tests;

public class ReferenceTests
{
    [Fact]
    public void Reference_evaluates_to_the_value_of_the_named_feature()
    {
        var expression = Expression.Reference("SzerokoscMM");
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(900m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(900m), value);
    }
}
