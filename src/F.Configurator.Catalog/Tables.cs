using System.Collections.Immutable;
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

internal sealed class SequenceComparer : IEqualityComparer<ImmutableList<Value>>
{
    public static readonly SequenceComparer Instance = new();
    public bool Equals(ImmutableList<Value>? x, ImmutableList<Value>? y) => x!.SequenceEqual(y!);

    public int GetHashCode(ImmutableList<Value> key) =>
        key.Aggregate(new HashCode(), (h, v) =>
        {
            h.Add(v);
            return h;
        }).ToHashCode();
}

public sealed class AllowedCombinationsTableBuilder
{
    private readonly AllowedCombinationsTable _table;

    private AllowedCombinationsTableBuilder(AllowedCombinationsTable table) => _table = table;

    public static AllowedCombinationsTableBuilder Create() =>
        new(new AllowedCombinationsTable(
            EquatableList<string>.Empty,
            string.Empty,
            EquatableDictionary<EquatableList<Value>, EquatableList<Value>>.Empty));

    public AllowedCombinationsTableBuilder Key(string key, params IEnumerable<string> keys) =>
        new(_table with { KeyColumns = _table.KeyColumns.Add(key).AddRange(keys) });

    public AllowedCombinationsTableBuilder Allowed(string feature) =>
        new(_table with { AllowedFeature = feature });

    public AllowedCombinationsTableBuilder Row(string cell, params IReadOnlyList<string> cells)
    {
        string[] all = [cell, .. cells];
        if (all.Length - _table.KeyColumns.Count != 1)
        {
            throw new ArgumentException(
                $"Row for '{_table.AllowedFeature}' needs {_table.KeyColumns.Count + 1} cells (keys + allowed option), got {all.Length}.");
        }

        EquatableList<Value> key = [.. all.SkipLast(1).Select(Value.Option)];
        var value = Value.Option(all[^1]);
        var allowed = _table.Rows.GetValueOrDefault(key, []);
        return new AllowedCombinationsTableBuilder(_table with { Rows = _table.Rows.SetItem(key, allowed.Add(value)) });
    }

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
        return new AllowedCombinationsTableBuilder(_table with { Rows = _table.Rows.SetItem(key, allowed.Add(value)) });
    }

    public AllowedCombinationsTable Build() => _table;
}
