namespace F.Configurator.Expressions.Tests;

// Grammar 4.2: `Model.GruboscMM` reads an attribute of the chosen option.
// The catalog compiler fills the map (option id -> attribute value), so expressions stay catalog-free.
public class OptionAttributeTests
{
    [Fact]
    public void Option_attribute_reads_the_value_of_the_chosen_option()
    {
        var gruboscMM = new Dictionary<string, Value>
        {
            ["PORTA"] = Value.Number(40m),
            ["VERTE"] = Value.Number(44m),
        };
        var expression = Expression.OptionAttribute(Expression.Reference("Model"), gruboscMM);
        var values = new Dictionary<string, Value> { ["Model"] = Value.Option("VERTE") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(44m), value);
    }

    // Grammar 6.4: nothing chosen yet, so the attribute has no value.
    [Fact]
    public void Option_attribute_is_missing_when_no_option_is_chosen()
    {
        var gruboscMM = new Dictionary<string, Value> { ["PORTA"] = Value.Number(40m) };
        var expression = Expression.OptionAttribute(Expression.Reference("Model"), gruboscMM);

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Missing, value);
    }

    // An option may leave an attribute unset (e.g. a glass-less model has no glass type).
    [Fact]
    public void Option_attribute_is_missing_when_the_chosen_option_does_not_set_it()
    {
        var gruboscMM = new Dictionary<string, Value> { ["PORTA"] = Value.Number(40m) };
        var expression = Expression.OptionAttribute(Expression.Reference("Model"), gruboscMM);
        var values = new Dictionary<string, Value> { ["Model"] = Value.Option("VERTE") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }
}
