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
}
