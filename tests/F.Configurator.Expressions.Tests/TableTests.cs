namespace F.Configurator.Expressions.Tests;

// Grammar 4.4 and 6.3: `Tabela[k1, k2].Kolumna` reads a value column from the row matching the keys.
// The node holds the table itself; the catalog compiler resolves the table name.
public class TableTests
{
    [Fact]
    public void Lookup_reads_the_column_from_the_matching_row()
    {
        var table = new Table(
            ["Norma"],
            [new TableRow([Value.Text("PL")], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) })]);
        var expression = Expression.TableLookup(table, [Expression.Reference("Norma")], "SzerokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Text("PL") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(844m), value);
    }

    // No row for the keys is a gap the validator reports; at runtime it is a missing value.
    [Fact]
    public void Lookup_without_a_matching_row_evaluates_to_missing()
    {
        var table = new Table(
            ["Norma"],
            [new TableRow([Value.Text("PL")], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) })]);
        var expression = Expression.TableLookup(table, [Expression.Reference("Norma")], "SzerokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Text("DE") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }

    // A column outside the table is reported by the validator; at runtime it is a missing value.
    [Fact]
    public void Lookup_of_an_unknown_column_evaluates_to_missing()
    {
        var table = new Table(
            ["Norma"],
            [new TableRow([Value.Text("PL")], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) })]);
        var expression = Expression.TableLookup(table, [Expression.Reference("Norma")], "WysokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Text("PL") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }
}
