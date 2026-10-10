namespace F.Configurator.Expressions.Tests;

// Grammar 4.4 and 6.3: `Tabela[k1, k2].Kolumna` reads a value column from the row matching the keys.
// The node holds the table itself; the catalog compiler resolves the table name.
// Key cells hold options of a choice feature (`Norma` is a choice: PL, DE, CZ, SK).
public class TableTests
{
    [Fact]
    public void Lookup_reads_the_column_from_the_matching_row()
    {
        var table = new Table(
            ["Norma"],
            [new TableRow([[Value.Option("PL")]], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) }.ToEquatableDictionary())]);
        var expression = TableLookup(table, [Reference("Norma")], "SzerokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Option("PL") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(844m), value);
    }

    // No row for the keys is a gap the validator reports; at runtime it is a missing value.
    [Fact]
    public void Lookup_without_a_matching_row_evaluates_to_missing()
    {
        var table = new Table(
            ["Norma"],
            [new TableRow([[Value.Option("PL")]], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) }.ToEquatableDictionary())]);
        var expression = TableLookup(table, [Reference("Norma")], "SzerokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Option("DE") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }

    // A column outside the table is reported by the validator; at runtime it is a missing value.
    [Fact]
    public void Lookup_of_an_unknown_column_evaluates_to_missing()
    {
        var table = new Table(
            ["Norma"],
            [new TableRow([[Value.Option("PL")]], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) }.ToEquatableDictionary())]);
        var expression = TableLookup(table, [Reference("Norma")], "WysokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Option("PL") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }

    // Grammar 4.4: a key cell may list several values (`CZ, SK`); the row matches any of them.
    [Theory]
    [InlineData("CZ")]
    [InlineData("SK")]
    public void Lookup_matches_any_of_the_values_listed_in_a_key_cell(string norma)
    {
        var table = new Table(
            ["Norma"],
            [new TableRow([[Value.Option("CZ"), Value.Option("SK")]], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(830m) }.ToEquatableDictionary())]);
        var expression = TableLookup(table, [Reference("Norma")], "SzerokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Option(norma) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(830m), value);
    }

    // Grammar 4.4: an empty key cell matches any chosen value.
    [Fact]
    public void Lookup_matches_any_value_in_an_empty_key_cell()
    {
        var table = new Table(
            ["Norma", "Szerokosc"],
            [new TableRow([[Value.Option("PL")], []], new Dictionary<string, Value> { ["WysokoscMM"] = Value.Number(2030m) }.ToEquatableDictionary())]);
        var expression = TableLookup(
            table,
            [Reference("Norma"), Reference("Szerokosc")],
            "WysokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Option("PL"), ["Szerokosc"] = Value.Number(90m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Number(2030m), value);
    }

    // "Any" means any chosen value: a feature without a value does not match an empty key cell (6.4).
    [Fact]
    public void Missing_key_does_not_match_an_empty_key_cell()
    {
        var table = new Table(
            ["Norma", "Szerokosc"],
            [new TableRow([[Value.Option("PL")], []], new Dictionary<string, Value> { ["WysokoscMM"] = Value.Number(2030m) }.ToEquatableDictionary())]);
        var expression = TableLookup(
            table,
            [Reference("Norma"), Reference("Szerokosc")],
            "WysokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Option("PL") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }

    // Every key column needs a key; a lookup with fewer keys must not match on the columns it has.
    [Fact]
    public void Lookup_with_fewer_keys_than_key_columns_evaluates_to_missing()
    {
        var table = new Table(
            ["Norma", "Szerokosc"],
            [new TableRow([[Value.Option("PL")], [Value.Number(90m)]], new Dictionary<string, Value> { ["WysokoscMM"] = Value.Number(2030m) }.ToEquatableDictionary())]);
        var expression = TableLookup(table, [Reference("Norma")], "WysokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Option("PL") };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }

    [Fact]
    public void Lookup_with_more_keys_than_key_columns_evaluates_to_missing()
    {
        var table = new Table(
            ["Norma"],
            [new TableRow([[Value.Option("PL")]], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) }.ToEquatableDictionary())]);
        var expression = TableLookup(
            table,
            [Reference("Norma"), Reference("Szerokosc")],
            "SzerokoscMM");
        var values = new Dictionary<string, Value> { ["Norma"] = Value.Option("PL"), ["Szerokosc"] = Value.Number(90m) };

        var value = expression.Evaluate(values);

        Assert.Equal(Value.Missing, value);
    }
}
