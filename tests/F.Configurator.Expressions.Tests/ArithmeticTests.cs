namespace F.Configurator.Expressions.Tests;

public class ArithmeticTests
{
    [Fact]
    public void Addition_of_two_numbers_evaluates_to_their_sum()
    {
        var expression = Expression.Add(Expression.Number(2m), Expression.Number(3m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Number(5m), value);
    }

    [Fact]
    public void Addition_evaluates_references_in_its_operands()
    {
        var expression = Expression.Add(Expression.Reference("SzerokoscMM"), Expression.Number(4m));
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(900m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(904m), value);
    }

    // Grammar 6.4: arithmetic with a missing value gives a missing value.
    [Theory]
    [InlineData(1)]
    [InlineData(-21)]
    [InlineData(0.5)]
    [InlineData(1044)]
    public void Addition_with_missing_operand_evaluates_to_missing(decimal amount)
    {
        var expression = Expression.Add(Expression.Reference("SzerokoscMM"), Expression.Number(amount));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Missing, value);
    }

    [Fact]
    public void Subtraction_evaluates_to_the_difference()
    {
        var expression = Expression.Subtract(Expression.Reference("SzerokoscMM"), Expression.Number(4m));
        var values = new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(900m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(896m), value);
    }

    [Fact]
    public void Multiplication_evaluates_to_the_product()
    {
        var expression = Expression.Multiply(Expression.Reference("SzerokoscMM"), Expression.Reference("WysokoscMM"));
        var values = new Dictionary<string, Value>
        {
            ["SzerokoscMM"] = Value.Number(900m),
            ["WysokoscMM"] = Value.Number(2.05m),
        };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(1845m), value);
    }

    [Fact]
    public void Division_evaluates_to_the_quotient()
    {
        var expression = Expression.Divide(Expression.Reference("PowierzchniaMM2"), Expression.Number(1000000m));
        var values = new Dictionary<string, Value> { ["PowierzchniaMM2"] = Value.Number(1845000m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(1.845m), value);
    }

    // Decision 2026-10-07: division by zero gives a missing value, like other undefined arithmetic (6.4).
    [Fact]
    public void Division_by_zero_evaluates_to_missing()
    {
        var expression = Expression.Divide(Expression.Number(1845m), Expression.Reference("Dzielnik"));
        var values = new Dictionary<string, Value> { ["Dzielnik"] = Value.Number(0m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }

    // Grammar 6.1 level 8: prefix minus.
    [Fact]
    public void Negation_evaluates_to_the_opposite_number()
    {
        var expression = Expression.Negate(Expression.Reference("Odchylka"));
        var values = new Dictionary<string, Value> { ["Odchylka"] = Value.Number(3m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(-3m), value);
    }

    // Like the binary operators: arithmetic is only defined on numbers, anything else evaluates to missing.
    [Fact]
    public void Negation_of_a_non_number_evaluates_to_missing()
    {
        var expression = Expression.Negate(Expression.Reference("MaRamiak"));
        var values = new Dictionary<string, Value> { ["MaRamiak"] = Value.Boolean(true) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }

    // Grammar 6.4 and the E2 rule "an expression never throws": a result outside the decimal range has
    // no value, like division by zero.
    [Fact]
    public void Addition_beyond_the_decimal_range_evaluates_to_missing()
    {
        var expression = Expression.Add(Expression.Number(decimal.MaxValue), Expression.Number(decimal.MaxValue));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Missing, value);
    }

    [Fact]
    public void Subtraction_beyond_the_decimal_range_evaluates_to_missing()
    {
        var expression = Expression.Subtract(Expression.Number(decimal.MinValue), Expression.Number(decimal.MaxValue));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Missing, value);
    }

    [Fact]
    public void Multiplication_beyond_the_decimal_range_evaluates_to_missing()
    {
        var expression = Expression.Multiply(Expression.Number(decimal.MaxValue), Expression.Number(10m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Missing, value);
    }

    [Fact]
    public void Division_beyond_the_decimal_range_evaluates_to_missing()
    {
        var expression = Expression.Divide(Expression.Number(decimal.MaxValue), Expression.Number(0.1m));

        var value = expression.Evaluate(new Dictionary<string, Value>());

        Assert.Equal(Value.Missing, value);
    }
}
