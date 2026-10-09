using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public sealed record Collection(string Name, EquatableList<Stage> Stages, CollectionMode Mode, Expression? Index)
{
    public EquatableList<string> Features => [.. Stages.SelectMany(s => s.Features)];
}

public sealed record Stage(string Name, EquatableList<string> Features);

public enum CollectionMode
{
    Sequential,
    Independent
}

public sealed class CollectionBuilder
{
    private readonly Collection _collection;

    private CollectionBuilder(Collection collection) => _collection = collection;

    public static CollectionBuilder Create(string name) =>
        new(new Collection(name, EquatableList<Stage>.Empty, CollectionMode.Sequential, null));

    public CollectionBuilder Stage(string stageName, string feature, params IEnumerable<string> features)
        => new(_collection with
        {
            Stages = _collection.Stages.Add(new Stage(stageName, [.. features.Prepend(feature)]))
        });

    public CollectionBuilder Independent() => new(_collection with { Mode = CollectionMode.Independent });

    public CollectionBuilder Index(Expression indexPattern) => new(_collection with { Index = indexPattern });

    public Collection Build() => _collection;
}
