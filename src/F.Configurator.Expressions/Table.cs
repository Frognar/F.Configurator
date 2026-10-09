namespace F.Configurator.Expressions;

public sealed record Table(IReadOnlyList<string> KeyColumns, IReadOnlyList<TableRow> Rows)
{
    public Value Lookup(IReadOnlyList<Value> keys, string valueKey)
    {
        if (KeyColumns.Count != keys.Count)
        {
            return Value.Missing;
        }

        var row = Rows.FirstOrDefault(r => r.Keys.Zip(keys).All(Matches));
        return row is not null && row.Values.TryGetValue(valueKey, out var value) ? value : Value.Missing;
    }

    private static bool Matches((IReadOnlyList<Value> cell, Value key) pair)
    {
        return pair.key is not MissingValue && (pair.cell.Count == 0 || pair.cell.Contains(pair.key));
    }
}

public sealed record TableRow(IReadOnlyList<IReadOnlyList<Value>> Keys, IReadOnlyDictionary<string, Value> Values);
