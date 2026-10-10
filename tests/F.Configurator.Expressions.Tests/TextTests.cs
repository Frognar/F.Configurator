namespace F.Configurator.Expressions.Tests;

// Grammar 6.2: text is a value type of its own, and `+` joins texts (used by the index).
public class TextTests
{
    [Fact]
    public void Addition_of_two_texts_joins_them()
    {
        var expression = Add(Reference("Seria"), Text("-90"));
        var values = new Dictionary<string, Value> { ["Seria"] = Value.Text("AST") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Text("AST-90"), value);
    }

    // Grammar 6.2: `+` joins texts or adds numbers, never mixes them.
    [Fact]
    public void Addition_of_text_and_number_evaluates_to_missing()
    {
        var expression = Add(Reference("Seria"), Number(90m));
        var values = new Dictionary<string, Value> { ["Seria"] = Value.Text("AST") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }

    [Fact]
    public void Equal_texts_compare_as_equal()
    {
        var expression = Equal(Reference("Seria"), Text("AST"));
        var values = new Dictionary<string, Value> { ["Seria"] = Value.Text("AST") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }
}
