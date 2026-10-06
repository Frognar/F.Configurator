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
}
