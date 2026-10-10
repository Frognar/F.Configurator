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
    private readonly EquatableList<string> _valueColumns;

    private TableBuilder(Table table, EquatableList<string> valueColumns) =>
        (_table, _valueColumns) = (table, valueColumns);

    public static TableBuilder Create(IEnumerable<string> keyColumns, IEnumerable<string> valueColumns) =>
        new(new Table([.. keyColumns], []), [.. valueColumns]);

    public TableBuilder Row(IEnumerable<Value> keyCells, IEnumerable<Value> valueCells) =>
        Row(keyCells.Select(k => (IEnumerable<Value>)[k]), valueCells);

    public TableBuilder Row(IEnumerable<IEnumerable<Value>> keyCells, IEnumerable<Value> valueCells)
    {
        var keys = EquatableList.Create(keyCells.Select(EquatableList.Create));
        var values = EquatableList.Create(valueCells);
        if (keys.Count != _table.KeyColumns.Count)
        {
            throw new ArgumentException(
                $"Row needs {_table.KeyColumns.Count} key cells ({string.Join(", ", _table.KeyColumns)}), got {keys.Count}.");
        }

        if (values.Count != _valueColumns.Count)
        {
            throw new ArgumentException(
                $"Row needs {_valueColumns.Count} value cells ({string.Join(", ", _valueColumns)}), got {values.Count}.");
        }

        return new TableBuilder(_table with
        {
            Rows = _table.Rows.Add(new TableRow(keys, _valueColumns.Zip(values).ToEquatableDictionary()))
        }, _valueColumns);
    }

    public Table Build() => _table;
}
