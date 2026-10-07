namespace F.Configurator.Expressions.Tests;

// Grammar 6.3: built-in functions.
public class FunctionTests
{
    [Fact]
    public void Min_evaluates_to_the_smallest_argument()
    {
        var expression = Expression.Min(
            Expression.Reference("SzerokoscMM"),
            Expression.Number(1000m),
            Expression.Reference("WysokoscMM"));
        var values = new Dictionary<string, Value>
        {
            ["SzerokoscMM"] = Value.Number(900m),
            ["WysokoscMM"] = Value.Number(2000m),
        };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(900m), value);
    }

    // Grammar 6.4: like arithmetic, a missing argument makes the result missing.
    [Fact]
    public void Min_with_a_missing_argument_evaluates_to_missing()
    {
        var expression = Expression.Min(Expression.Reference("SzerokoscMM"), Expression.Number(1000m));
        var values = new Dictionary<string, Value>();

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }

    [Fact]
    public void Max_evaluates_to_the_largest_argument()
    {
        var expression = Expression.Max(
            Expression.Reference("SzerokoscMM"),
            Expression.Number(1000m),
            Expression.Reference("WysokoscMM"));
        var values = new Dictionary<string, Value>
        {
            ["SzerokoscMM"] = Value.Number(900m),
            ["WysokoscMM"] = Value.Number(2000m),
        };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(2000m), value);
    }

    [Fact]
    public void Length_evaluates_to_the_number_of_characters()
    {
        var expression = Expression.Length(Expression.Reference("Grawer"));
        var values = new Dictionary<string, Value> { ["Grawer"] = Value.Text("Kowalscy") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(8m), value);
    }

    // Grammar, index section: `dopełnij(x, n)` pads with zeros on the left, e.g. `dopełnij(WysokoscMM, 4)`.
    [Fact]
    public void Pad_fills_a_number_with_zeros_on_the_left()
    {
        var expression = Expression.Pad(Expression.Reference("WysokoscMM"), Expression.Number(4m));
        var values = new Dictionary<string, Value> { ["WysokoscMM"] = Value.Number(900m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Text("0900"), value);
    }

    // A decimal keeps its scale (900 * 1.00 = 900.00); the index must not depend on it.
    [Fact]
    public void Pad_ignores_trailing_zeros_of_the_number()
    {
        var expression = Expression.Pad(
            Expression.Multiply(Expression.Reference("WysokoscMM"), Expression.Number(1.00m)),
            Expression.Number(4m));
        var values = new Dictionary<string, Value> { ["WysokoscMM"] = Value.Number(900m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Text("0900"), value);
    }
}
