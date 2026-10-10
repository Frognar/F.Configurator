namespace F.Configurator.Expressions;

public sealed record Table(EquatableList<string> KeyColumns, EquatableList<TableRow> Rows)
{
    public Value Lookup(EquatableList<Value> keys, string valueKey)
    {
        if (KeyColumns.Count != keys.Count)
        {
            return Value.Missing;
        }

        var row = Rows.FirstOrDefault(r => r.Keys.Zip(keys).All(Matches));
        return row is not null && row.Values.TryGetValue(valueKey, out var value) ? value : Value.Missing;
    }

    private static bool Matches((EquatableList<Value> cell, Value key) pair)
    {
        return pair.key is not MissingValue && (pair.cell.Count == 0 || pair.cell.Contains(pair.key));
    }
}

public sealed record TableRow(EquatableList<EquatableList<Value>> Keys, EquatableDictionary<string, Value> Values);

public sealed class TableBuilder
{
    private readonly Table _table;

    private TableBuilder(Table table) => _table = table;

    public static TableBuilder Create(IEnumerable<string> x, IEnumerable<string> y) => new(new Table([], []));

    public TableBuilder Row(IEnumerable<Value> x, IEnumerable<Value> y) => new(_table);

    public TableBuilder Row(IEnumerable<IEnumerable<Value>> x, IEnumerable<Value> y) => new(_table);

    public Table Build() => _table;
}
