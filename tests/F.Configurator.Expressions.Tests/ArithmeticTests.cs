namespace F.Configurator.Expressions.Tests;

public class ArithmeticTests
{
    [Fact]
    public void Addition_of_two_numbers_evaluates_to_their_sum()
    {
        var expression = Expression.Add(Expression.Number(2m), Expression.Number(3m));

        var value = expression.Evaluate();

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
}
