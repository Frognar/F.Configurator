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

    [Fact]
    public void Pad_fills_a_text_with_zeros_on_the_left()
    {
        var expression = Expression.Pad(Expression.Reference("Kod"), Expression.Number(3m));
        var values = new Dictionary<string, Value> { ["Kod"] = Value.Text("7") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Text("007"), value);
    }

    // `dopełnij(x, n, znak)` pads with the given character instead of zeros.
    [Fact]
    public void Pad_fills_with_the_given_character()
    {
        var expression = Expression.Pad(Expression.Reference("Kod"), Expression.Number(4m), Expression.Text("_"));
        var values = new Dictionary<string, Value> { ["Kod"] = Value.Text("AB") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Text("__AB"), value);
    }

    // The fill must be exactly one character; anything else is reported by the validator.
    [Theory]
    [InlineData("")]
    [InlineData("_-")]
    public void Pad_with_a_fill_other_than_one_character_evaluates_to_missing(string fill)
    {
        var expression = Expression.Pad(Expression.Reference("Kod"), Expression.Number(4m), Expression.Text(fill));
        var values = new Dictionary<string, Value> { ["Kod"] = Value.Text("AB") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }

    // `zaokrąglij(x)` rounds half up, like Excel (decision 2026-10-07).
    [Fact]
    public void Round_rounds_half_up()
    {
        var expression = Expression.Round(Expression.Number(2.5m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Number(3m), value);
    }

    // Half up means away from zero for negatives too, as in Excel.
    [Fact]
    public void Round_rounds_negative_half_away_from_zero()
    {
        var expression = Expression.Round(Expression.Number(-2.5m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Number(-3m), value);
    }

    // `zaokrąglij(x, krok)` rounds to the nearest multiple of the step, half up.
    [Fact]
    public void Round_to_a_step_rounds_to_the_nearest_multiple()
    {
        var expression = Expression.Round(Expression.Number(1234m), Expression.Number(10m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Number(1230m), value);
    }

    // A step works for prices too, e.g. rounding to 5 groszy.
    [Fact]
    public void Round_to_a_fractional_step_rounds_half_up()
    {
        var expression = Expression.Round(Expression.Number(1.225m), Expression.Number(0.05m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Number(1.25m), value);
    }

    // A step must be positive; anything else is reported by the validator.
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Round_to_a_non_positive_step_evaluates_to_missing(int step)
    {
        var expression = Expression.Round(Expression.Number(1234m), Expression.Number(step));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Missing, value);
    }

    // The E2 rule "an expression never throws": rounding to a step that pushes the quotient beyond
    // the decimal range has no value.
    [Fact]
    public void Round_beyond_the_decimal_range_evaluates_to_missing()
    {
        var expression = Expression.Round(Expression.Number(decimal.MaxValue), Expression.Number(0.1m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Missing, value);
    }

    // Indexes are tens of characters long, so `dopełnij` is capped at 1000 characters (agreed 2026-10-09).
    // A wider pad has no value instead of overflowing int or allocating a huge string.
    [Theory]
    [InlineData(1001)]
    [InlineData(100000000000000000000.0)]
    public void Pad_wider_than_the_limit_evaluates_to_missing(double width)
    {
        var expression = Expression.Pad(Expression.Text("12"), Expression.Number((decimal)width));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Missing, value);
    }

    [Fact]
    public void Pad_up_to_the_limit_is_allowed()
    {
        var expression = Expression.Pad(Expression.Text("12"), Expression.Number(1000m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Text("12".PadLeft(1000, '0')), value);
    }
}
