namespace F.Configurator.Expressions.Tests;

public class ValueTests
{
    [Fact]
    public void Numbers_with_different_amounts_are_not_equal()
    {
        Assert.NotEqual(Value.Number(1m), Value.Number(2m));
    }
}
