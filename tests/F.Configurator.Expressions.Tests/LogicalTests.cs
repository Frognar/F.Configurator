namespace F.Configurator.Expressions.Tests;

// Grammar 6.1 levels 2-4: `lub`, `i`, `nie`.
public class LogicalTests
{
    [Fact]
    public void And_of_two_true_conditions_is_true()
    {
        var expression = And(
            GreaterThan(Reference("SzerokoscMM"), Number(800m)),
            LessThan(Reference("WysokoscMM"), Number(2100m)));
        var values = new Dictionary<string, Value>
        {
            ["SzerokoscMM"] = Value.Number(900m),
            ["WysokoscMM"] = Value.Number(2000m),
        };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void And_with_a_false_condition_is_false(bool left, bool right)
    {
        var expression = And(Reference("A"), Reference("B"));
        var values = new Dictionary<string, Value> { ["A"] = Value.Boolean(left), ["B"] = Value.Boolean(right) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }

    // Grammar 6.4: a condition with a missing value is false.
    [Fact]
    public void And_with_a_missing_condition_is_false()
    {
        var expression = And(Reference("A"), Reference("B"));
        var values = new Dictionary<string, Value> { ["B"] = Value.Boolean(true) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }

    [Fact]
    public void Or_with_one_true_condition_is_true()
    {
        var expression = Or(Reference("A"), Reference("B"));
        var values = new Dictionary<string, Value> { ["A"] = Value.Boolean(false), ["B"] = Value.Boolean(true) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Grammar 6.4: a missing condition counts as false, so only a true one makes `lub` true.
    [Fact]
    public void Or_without_a_true_condition_is_false()
    {
        var expression = Or(Reference("A"), Reference("B"));
        var values = new Dictionary<string, Value> { ["B"] = Value.Boolean(false) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }

    [Fact]
    public void Not_negates_a_condition()
    {
        var expression = Not(Reference("A"));
        var values = new Dictionary<string, Value> { ["A"] = Value.Boolean(true) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Boolean(false), value);
    }

    // Grammar 6.4: a missing condition counts as false, so its negation is true, consistent with `≠` and `nie w`.
    [Fact]
    public void Not_of_a_missing_condition_is_true()
    {
        var expression = Not(Reference("A"));

        var value = expression.Evaluate(NoValues);

        Assert.Equal(Value.Boolean(true), value);
    }

    // Agreed 2026-10-09: `i`, `lub` and `jeśli` treat any value other than true as false, so `nie`
    // negates that view: `nie x` is true for everything except true. The validator rejects such
    // operands; this keeps runtime behaviour consistent.
    [Fact]
    public void Not_of_a_number_is_true()
    {
        var expression = Not(Number(5m));

        var value = expression.Evaluate(NoValues);

        Assert.Equal(Value.Boolean(true), value);
    }

    [Fact]
    public void Not_of_a_text_is_true()
    {
        var expression = Not(Text("krowa"));

        var value = expression.Evaluate(NoValues);

        Assert.Equal(Value.Boolean(true), value);
    }
}
