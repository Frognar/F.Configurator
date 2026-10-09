using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace F.Configurator.Expressions;

public sealed class EquatableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>,
    IEquatable<EquatableDictionary<TKey, TValue>> where TKey : notnull
{
    public static readonly EquatableDictionary<TKey, TValue> Empty = new(ImmutableDictionary<TKey, TValue>.Empty);
    private readonly ImmutableDictionary<TKey, TValue> _keyValuePairs;

    internal EquatableDictionary(ImmutableDictionary<TKey, TValue> keyValuePairs) => _keyValuePairs = keyValuePairs;

    public int Count => _keyValuePairs.Count;

    public IEnumerable<TKey> Keys => _keyValuePairs.Keys;

    public IEnumerable<TValue> Values => _keyValuePairs.Values;

    public TValue this[TKey key] => _keyValuePairs[key];

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _keyValuePairs.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool ContainsKey(TKey key) => _keyValuePairs.ContainsKey(key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) =>
        _keyValuePairs.TryGetValue(key, out value);

    public bool Equals(EquatableDictionary<TKey, TValue>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other) || ReferenceEquals(_keyValuePairs, other._keyValuePairs)) return true;
        if (_keyValuePairs.Count != other._keyValuePairs.Count) return false;

        var valueComparer = EqualityComparer<TValue>.Default;
        return _keyValuePairs.All(kvp =>
            other._keyValuePairs.TryGetValue(kvp.Key, out var otherValue)
            && valueComparer.Equals(kvp.Value, otherValue));
    }

    public override bool Equals(object? obj) => Equals(obj as EquatableDictionary<TKey, TValue>);

    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var (key, value) in _keyValuePairs)
        {
            hash = unchecked(hash + HashCode.Combine(key, value));
        }

        return HashCode.Combine(_keyValuePairs.Count, hash);
    }

    public override string ToString() =>
        "{" + string.Join(", ", _keyValuePairs
            .Select(kvp => $"{kvp.Key}: {kvp.Value}")
            .Order(StringComparer.Ordinal)) + "}";

    public EquatableDictionary<TKey, TValue> Add(TKey key, TValue value) => new(_keyValuePairs.Add(key, value));
    public EquatableDictionary<TKey, TValue> SetItem(TKey key, TValue value) => new(_keyValuePairs.SetItem(key, value));
}

public static class EquatableDictionary
{
    public static EquatableDictionary<TKey, TValue> ToEquatableDictionary<TKey, TValue>(
        this IEnumerable<KeyValuePair<TKey, TValue>> pairs) where TKey : notnull => new(pairs.ToImmutableDictionary());
}
