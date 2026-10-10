namespace F.Configurator.Expressions.Tests;

public class ValueTests
{
    [Fact]
    public void Numbers_with_different_amounts_are_not_equal()
    {
        Assert.NotEqual(Value.Number(1m), Value.Number(2m));
    }

    [Fact]
    public void Missing_is_not_equal_to_zero()
    {
        Assert.NotEqual(Value.Number(0m), Value.Missing);
    }

    // Records compare collections by reference unless told otherwise; a list value compares by its items.
    [Fact]
    public void Lists_with_the_same_items_are_equal()
    {
        StructuralAssert.Equal(
            Value.List(Value.Option("CZ"), Value.Option("SK")),
            Value.List(Value.Option("CZ"), Value.Option("SK")));
    }

    [Fact]
    public void Lists_with_items_in_a_different_order_are_not_equal()
    {
        Assert.NotEqual(
            Value.List(Value.Option("CZ"), Value.Option("SK")),
            Value.List(Value.Option("SK"), Value.Option("CZ")));
    }
}
