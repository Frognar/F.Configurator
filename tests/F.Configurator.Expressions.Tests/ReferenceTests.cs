namespace F.Configurator.Expressions.Tests;

public class ReferenceTests
{
    [Fact]
    public void Reference_evaluates_to_the_value_of_the_named_feature()
    {
        var expression = Reference("SzerokoscMM");
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(900m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(900m), value);
    }

    // Grammar 6.4: a feature may have no value yet (e.g. before it is chosen).
    [Fact]
    public void Reference_to_feature_without_value_evaluates_to_missing()
    {
        var expression = Reference("SzerokoscMM");

        var value = expression.Evaluate(NoValues);

        Assert.Equal(Value.Missing, value);
    }
}
