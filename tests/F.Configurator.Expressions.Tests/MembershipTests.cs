namespace F.Configurator.Expressions.Tests;

// Grammar 6.1 level 5: `w` and `nie w`, against a closed range (`-5..-1`) or a list (`[CZ, SK]`).
public class MembershipTests
{
    [Fact]
    public void Number_inside_range_is_in_the_range()
    {
        var expression = Expression.In(Expression.Reference("Odchylka"), Expression.Range(-5m, -1m));
        var values = new Dictionary<string, Value> { ["Odchylka"] = Value.Number(-3m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // The range is closed on both ends.
    [Theory]
    [InlineData(-5, true)]
    [InlineData(-1, true)]
    [InlineData(-6, false)]
    [InlineData(0, false)]
    public void Range_includes_both_bounds_and_nothing_outside(int odchylka, bool expected)
    {
        var expression = Expression.In(Expression.Reference("Odchylka"), Expression.Range(-5m, -1m));
        var values = new Dictionary<string, Value> { ["Odchylka"] = Value.Number(odchylka) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(expected), value);
    }

    // Grammar 6.4: a missing value is not in anything.
    [Fact]
    public void Missing_value_is_not_in_a_range()
    {
        var expression = Expression.In(Expression.Reference("Odchylka"), Expression.Range(-5m, -1m));
        var values = new Dictionary<string, Value>();

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }

    [Fact]
    public void Number_outside_range_is_not_in_the_range()
    {
        var expression = Expression.NotIn(Expression.Reference("Odchylka"), Expression.Range(-5m, -1m));
        var values = new Dictionary<string, Value> { ["Odchylka"] = Value.Number(2m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Grammar 6.4: like `≠`, "A nie w X" is true when A has no value.
    [Fact]
    public void Missing_value_is_outside_every_range()
    {
        var expression = Expression.NotIn(Expression.Reference("Odchylka"), Expression.Range(-5m, -1m));
        var values = new Dictionary<string, Value>();

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Grammar 6.1: `Kolekcja w [Brilliant, Basic]`.
    [Theory]
    [InlineData("Basic", true)]
    [InlineData("Brilliant", true)]
    [InlineData("Contrast", false)]
    public void Option_is_in_a_list_when_the_list_names_it(string kolekcja, bool expected)
    {
        var expression = Expression.In(
            Expression.Reference("Kolekcja"),
            Expression.List(Expression.Option("Brilliant"), Expression.Option("Basic")));
        var values = new Dictionary<string, Value> { ["Kolekcja"] = Value.Option(kolekcja) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(expected), value);
    }

    [Fact]
    public void Number_can_be_in_a_list_of_numbers()
    {
        var expression = Expression.In(
            Expression.Reference("Szerokosc"),
            Expression.List(Expression.Number(80m), Expression.Number(90m)));
        var values = new Dictionary<string, Value> { ["Szerokosc"] = Value.Number(90m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Grammar 2.4: `Model nie w [ANATOLIA, STANDARD_02]`.
    [Theory]
    [InlineData("PORTA", true)]
    [InlineData("ANATOLIA", false)]
    public void Option_is_not_in_a_list_that_does_not_name_it(string model, bool expected)
    {
        var expression = Expression.NotIn(
            Expression.Reference("Model"),
            Expression.List(Expression.Option("ANATOLIA"), Expression.Option("STANDARD_02")));
        var values = new Dictionary<string, Value> { ["Model"] = Value.Option(model) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(expected), value);
    }

    // Grammar 6.4: nothing chosen is not in a list, and "is not in" holds.
    [Fact]
    public void Missing_value_is_not_in_a_list()
    {
        var list = Expression.List(Expression.Option("CZ"), Expression.Option("SK"));
        var empty = new Dictionary<string, Value>();

        Assert.Equal(Value.Boolean(false), Expression.In(Expression.Reference("Norma"), list).Evaluate(empty));
        Assert.Equal(Value.Boolean(true), Expression.NotIn(Expression.Reference("Norma"), list).Evaluate(empty));
    }

    // Items are expressions, so a list may hold a reference; a missing item matches nothing.
    [Fact]
    public void List_items_are_evaluated()
    {
        var expression = Expression.In(
            Expression.Reference("SzerokoscMM"),
            Expression.List(Expression.Reference("SzerokoscDomyslnaMM"), Expression.Reference("Brak")));
        var values = new Dictionary<string, Value>
        {
            ["SzerokoscMM"] = Value.Number(844m),
            ["SzerokoscDomyslnaMM"] = Value.Number(844m),
        };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Grammar 6.4: a missing value matches nothing, not even a list item that is itself missing.
    [Fact]
    public void Missing_value_is_not_in_a_list_with_a_missing_item()
    {
        var list = Expression.List(Expression.Option("CZ"), Expression.Reference("Brak"));
        var empty = new Dictionary<string, Value>();

        Assert.Equal(Value.Boolean(false), Expression.In(Expression.Reference("Norma"), list).Evaluate(empty));
        Assert.Equal(Value.Boolean(true), Expression.NotIn(Expression.Reference("Norma"), list).Evaluate(empty));
    }

    // Agreed 2026-10-09: `x nie w Y` is always `nie (x w Y)`. A value of another type is not in a
    // range, so it is outside it.
    [Fact]
    public void Text_is_outside_a_number_range()
    {
        var expression = Expression.NotIn(Expression.Text("A"), Expression.Range(1m, 5m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Boolean(true), value);
    }

    [Fact]
    public void Option_is_outside_a_number_range()
    {
        var expression = Expression.NotIn(Expression.Option("CZ"), Expression.Range(1m, 5m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Boolean(true), value);
    }

    // The right side is neither a range nor a list, so nothing is in it.
    [Fact]
    public void Value_is_outside_something_that_is_neither_a_range_nor_a_list()
    {
        var expression = Expression.NotIn(Expression.Number(5m), Expression.Number(5m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Boolean(true), value);
    }
}
