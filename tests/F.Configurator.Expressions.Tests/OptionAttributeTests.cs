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
}
