using System.Globalization;

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

    // Decided 2026-10-13: `+` with text on one side joins the number to it, as `SzerokoscMM` in the Basic index.
    [Fact]
    public void Addition_of_text_and_number_joins_them()
    {
        var expression = Add(Reference("Seria"), Number(90m));
        var values = new Dictionary<string, Value> { ["Seria"] = Value.Text("AST") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Text("AST90"), value);
    }

    [Fact]
    public void Addition_of_number_and_text_joins_them()
    {
        var expression = Add(Number(90m), Reference("Seria"));
        var values = new Dictionary<string, Value> { ["Seria"] = Value.Text("AST") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Text("90AST"), value);
    }

    // The number is written as in `dopełnij`: invariant culture, no trailing zeros.
    [Theory]
    [InlineData("844.0", "Z844")]
    [InlineData("12.50", "Z12.5")]
    [InlineData("-21", "Z-21")]
    public void Number_joined_to_text_is_written_without_trailing_zeros(string number, string expected)
    {
        var expression = Add(Text("Z"), Number(decimal.Parse(number, CultureInfo.InvariantCulture)));

        var value = expression.Evaluate(TestValues.NoValues);

        Assert.Equal(Value.Text(expected), value);
    }

    // Grammar 6.4: a missing value stays missing, so an index with an unset feature has no value.
    [Fact]
    public void Addition_of_text_and_missing_value_evaluates_to_missing()
    {
        var expression = Add(Text("AST"), Reference("SzerokoscMM"));

        var value = expression.Evaluate(TestValues.NoValues);

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
