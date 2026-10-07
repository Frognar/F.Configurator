namespace F.Configurator.Expressions.Tests;

// Grammar 6.2: text is a value type of its own, and `+` joins texts (used by the index).
public class TextTests
{
    [Fact]
    public void Text_literal_evaluates_to_itself()
    {
        var expression = Expression.Text("ASTORIA");
        var values = new Dictionary<string, Value>();

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Text("ASTORIA"), value);
    }
}
