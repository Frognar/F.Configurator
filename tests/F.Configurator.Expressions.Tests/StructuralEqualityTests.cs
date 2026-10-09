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

    private static Table Wymiary() => new(
        ["Norma"],
        [new TableRow([[Value.Option("PL")]], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) })]);

    private static Dictionary<string, Value> GruboscMM() => new()
    {
        ["PORTA"] = Value.Number(40m),
        ["VERTE"] = Value.Number(44m),
    };

    [Fact]
    public void Tables_built_the_same_way_are_equal()
    {
        var first = Wymiary();
        var second = Wymiary();

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Table_lookups_built_the_same_way_are_equal()
    {
        var first = Expression.TableLookup(Wymiary(), [Expression.Reference("Norma")], "SzerokoscMM");
        var second = Expression.TableLookup(Wymiary(), [Expression.Reference("Norma")], "SzerokoscMM");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Option_attributes_built_the_same_way_are_equal()
    {
        var first = Expression.OptionAttribute(Expression.Reference("Model"), GruboscMM());
        var second = Expression.OptionAttribute(Expression.Reference("Model"), GruboscMM());

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    // Dictionaries compare by content, not by insertion order.
    [Fact]
    public void Option_attributes_with_values_added_in_a_different_order_are_equal()
    {
        var first = Expression.OptionAttribute(
            Expression.Reference("Model"),
            new Dictionary<string, Value> { ["PORTA"] = Value.Number(40m), ["VERTE"] = Value.Number(44m) });
        var second = Expression.OptionAttribute(
            Expression.Reference("Model"),
            new Dictionary<string, Value> { ["VERTE"] = Value.Number(44m), ["PORTA"] = Value.Number(40m) });

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
