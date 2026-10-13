namespace F.Configurator.Expressions.Tests;

// Grammar 6.3: `domyślna(Cecha)` is the default value of a feature, whatever its current value. Expressions stay
// free of the engine: the engine passes the defaults among the values, under the key `DefaultOf.KeyOf(feature)`.
public class DefaultOfTests
{
    [Fact]
    public void Default_of_a_feature_is_its_default_not_its_current_value()
    {
        var values = new Dictionary<string, Value>
        {
            ["SzerokoscMM"] = Value.Number(1100m),
            [DefaultOf.KeyOf("SzerokoscMM")] = Value.Number(944m),
        };

        Assert.Equal(Value.Number(944m), DefaultOf("SzerokoscMM").Evaluate(values));
    }

    [Fact]
    public void Default_of_a_feature_without_a_default_is_missing()
    {
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(1100m) };

        Assert.Equal(Value.Missing, DefaultOf("SzerokoscMM").Evaluate(values));
    }

    // The key is no feature name, so a default never hides a feature's own value.
    [Fact]
    public void Key_of_a_default_is_not_the_feature_name()
    {
        Assert.NotEqual("SzerokoscMM", DefaultOf.KeyOf("SzerokoscMM"));
    }
}
