using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace F.Configurator.Expressions;

// An immutable list that compares by its items, in order, so records holding it get structural equality.
[CollectionBuilder(typeof(EquatableList), nameof(EquatableList.Create))]
public sealed class EquatableList<T> : IReadOnlyList<T>, IEquatable<EquatableList<T>>
{
    private readonly ImmutableList<T> _items;

    internal EquatableList(ImmutableList<T> items) => _items = items;

    public int Count => _items.Count;

    public T this[int index] => _items[index];

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(EquatableList<T>? other) =>
        other is not null && (ReferenceEquals(this, other) || _items.SequenceEqual(other._items));

    public override bool Equals(object? obj) => Equals(obj as EquatableList<T>);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _items) hash.Add(item);
        return hash.ToHashCode();
    }

    public override string ToString() => $"[{string.Join(", ", _items)}]";
}

public static class EquatableList
{
    public static EquatableList<T> Create<T>(ReadOnlySpan<T> items) => new([.. items]);
}
