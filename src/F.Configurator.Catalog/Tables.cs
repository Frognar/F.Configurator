using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public sealed record AllowedCombinationsTable(
    EquatableList<string> KeyColumns,
    string AllowedFeature,
    EquatableDictionary<EquatableList<Value>, EquatableList<Value>> Rows)
{
    public EquatableList<Value>? AllowedFor(EquatableList<Value> key) =>
        key.Any(k => k is MissingValue) ? null : Rows.GetValueOrDefault(key, []);
}

public sealed class AllowedCombinationsTableBuilder
{
    private readonly AllowedCombinationsTable _table;

    private AllowedCombinationsTableBuilder(AllowedCombinationsTable table) => _table = table;

    public static AllowedCombinationsTableBuilder Create(IEnumerable<string> key, string allowed) =>
        new(new AllowedCombinationsTable(
            [..key],
            allowed,
            EquatableDictionary<EquatableList<Value>, EquatableList<Value>>.Empty));

    public AllowedCombinationsTableBuilder Row(string cell, params IReadOnlyList<string> cells)
        => Row(Value.Option(cell), [.. cells.Select(Value.Option)]);

    public AllowedCombinationsTableBuilder Row(Value cell, params IReadOnlyList<Value> cells)
    {
        Value[] all = [cell, .. cells];
        if (all.Length - _table.KeyColumns.Count != 1)
        {
            throw new ArgumentException(
                $"Row for '{_table.AllowedFeature}' needs {_table.KeyColumns.Count + 1} cells (keys + allowed option), got {all.Length}.");
        }

        EquatableList<Value> key = [.. all.SkipLast(1)];
        var value = all[^1];
        var allowed = _table.Rows.GetValueOrDefault(key, []);
        return allowed.Contains(value)
            ? this
            : new AllowedCombinationsTableBuilder(_table with { Rows = _table.Rows.SetItem(key, allowed.Add(value)) });
    }

    public AllowedCombinationsTable Build() => _table;
}
