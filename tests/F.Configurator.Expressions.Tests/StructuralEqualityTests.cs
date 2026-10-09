namespace F.Configurator.Expressions.Tests;

// The parser (E5) and the catalog compiler will be tested by comparing whole expression trees,
// so nodes that hold several sub-expressions must compare by content, not by reference.
public class StructuralEqualityTests
{
    [Fact]
    public void Min_nodes_built_the_same_way_are_equal()
    {
        var first = Expression.Min(Expression.Reference("SzerokoscMM"), Expression.Number(900m));
        var second = Expression.Min(Expression.Reference("SzerokoscMM"), Expression.Number(900m));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Max_nodes_built_the_same_way_are_equal()
    {
        var first = Expression.Max(Expression.Reference("SzerokoscMM"), Expression.Number(900m));
        var second = Expression.Max(Expression.Reference("SzerokoscMM"), Expression.Number(900m));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void List_nodes_built_the_same_way_are_equal()
    {
        var first = Expression.List(Expression.Option("CZ"), Expression.Option("SK"));
        var second = Expression.List(Expression.Option("CZ"), Expression.Option("SK"));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
