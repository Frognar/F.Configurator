namespace F.Configurator.Expressions.Tests;

// The parser (E5) and the catalog compiler will be tested by comparing whole expression trees,
// so nodes that hold several sub-expressions must compare by content, not by reference.
public class StructuralEqualityTests
{
    [Fact]
    public void Min_nodes_built_the_same_way_are_equal()
    {
        StructuralAssert.Equal(
            Min(Reference("SzerokoscMM"), Number(900m)),
            Min(Reference("SzerokoscMM"), Number(900m)));
    }

    [Fact]
    public void Max_nodes_built_the_same_way_are_equal()
    {
        StructuralAssert.Equal(
            Max(Reference("SzerokoscMM"), Number(900m)),
            Max(Reference("SzerokoscMM"), Number(900m)));
    }

    [Fact]
    public void List_nodes_built_the_same_way_are_equal()
    {
        StructuralAssert.Equal(
            Expression.List(Option("CZ"), Option("SK")),
            Expression.List(Option("CZ"), Option("SK")));
    }

    private static Table Wymiary() => new(
        ["Norma"],
        [new TableRow([[Value.Option("PL")]], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) }.ToEquatableDictionary())]);

    private static EquatableDictionary<string, Value> GruboscMM() => new Dictionary<string, Value>
    {
        ["PORTA"] = Value.Number(40m),
        ["VERTE"] = Value.Number(44m),
    }.ToEquatableDictionary();

    [Fact]
    public void Tables_built_the_same_way_are_equal()
    {
        StructuralAssert.Equal(Wymiary(), Wymiary());
    }

    [Fact]
    public void Table_lookups_built_the_same_way_are_equal()
    {
        StructuralAssert.Equal(
            TableLookup(Wymiary(), [Reference("Norma")], "SzerokoscMM"),
            TableLookup(Wymiary(), [Reference("Norma")], "SzerokoscMM"));
    }

    [Fact]
    public void Option_attributes_built_the_same_way_are_equal()
    {
        StructuralAssert.Equal(
            OptionAttribute(Reference("Model"), GruboscMM()),
            OptionAttribute(Reference("Model"), GruboscMM()));
    }

    // Dictionaries compare by content, not by insertion order.
    [Fact]
    public void Option_attributes_with_values_added_in_a_different_order_are_equal()
    {
        var first = OptionAttribute(
            Reference("Model"),
            new Dictionary<string, Value> { ["PORTA"] = Value.Number(40m), ["VERTE"] = Value.Number(44m) }.ToEquatableDictionary());
        var second = OptionAttribute(
            Reference("Model"),
            new Dictionary<string, Value> { ["VERTE"] = Value.Number(44m), ["PORTA"] = Value.Number(40m) }.ToEquatableDictionary());

        StructuralAssert.Equal(first, second);
    }
}
