namespace F.Configurator.Expressions.Tests;

// Grammar 4.4: a table is written like a sheet: a header with key columns and value columns, then rows
// of cells in the same order. The builder reads like that, so a test shows the table, not its records.
public class TableBuilderTests
{
    [Fact]
    public void Builder_keeps_the_key_columns()
    {
        var table = TableBuilder.Create(["Norma", "Szerokosc"], ["WysokoscMM"]).Build();

        Assert.Equal(["Norma", "Szerokosc"], table.KeyColumns);
        Assert.Empty(table.Rows);
    }

    // Value cells are matched to value columns by position.
    [Fact]
    public void Row_names_its_values_after_the_value_columns()
    {
        var table = TableBuilder.Create(["Norma"], ["SzerokoscMM", "WysokoscMM"])
            .Row([Value.Option("PL")], [Value.Number(844m), Value.Number(2030m)])
            .Build();

        var expected = new TableRow(
            [[Value.Option("PL")]],
            new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m), ["WysokoscMM"] = Value.Number(2030m) }.ToEquatableDictionary());
        Assert.Equal([expected], table.Rows);
    }

    // Grammar 4.4: a key cell may list several values (`CZ, SK`) or be empty (any value).
    [Fact]
    public void Row_keeps_key_cells_with_several_values_or_none()
    {
        var table = TableBuilder.Create(["Norma", "Szerokosc"], ["WysokoscMM"])
            .Row([[Value.Option("CZ"), Value.Option("SK")], []], [Value.Number(1970m)])
            .Build();

        Assert.Equal([[Value.Option("CZ"), Value.Option("SK")], []], Assert.Single(table.Rows).Keys);
    }

    [Fact]
    public void Builder_keeps_rows_in_order()
    {
        var table = TableBuilder.Create(["Norma"], ["SzerokoscMM"])
            .Row([Value.Option("PL")], [Value.Number(844m)])
            .Row([Value.Option("DE")], [Value.Number(860m)])
            .Build();

        Assert.Equal([Value.Number(844m), Value.Number(860m)], table.Rows.Select(row => row.Values["SzerokoscMM"]));
    }

    // Like `AllowedCombinationsTableBuilder`: a row that does not fit the header is a typo in code that
    // builds the catalog.
    [Fact]
    public void Row_with_a_wrong_number_of_key_cells_is_rejected()
    {
        var builder = TableBuilder.Create(["Norma", "Szerokosc"], ["WysokoscMM"]);

        Assert.Throws<ArgumentException>(() => builder.Row([Value.Option("PL")], [Value.Number(2030m)]));
    }

    [Fact]
    public void Row_with_a_wrong_number_of_value_cells_is_rejected()
    {
        var builder = TableBuilder.Create(["Norma"], ["SzerokoscMM", "WysokoscMM"]);

        Assert.Throws<ArgumentException>(() => builder.Row([Value.Option("PL")], [Value.Number(844m)]));
    }
}
