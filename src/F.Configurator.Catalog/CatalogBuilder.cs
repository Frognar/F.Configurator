using System.Collections.Immutable;
using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public sealed class CatalogBuilder
{
    private readonly string _name;
    private readonly ImmutableList<Feature> _features;
    private readonly ImmutableList<Collection> _collections;
    private readonly ImmutableDictionary<string, Table> _tables;

    private CatalogBuilder(string name,
        ImmutableList<Feature> features,
        ImmutableList<Collection> collections,
        ImmutableDictionary<string, Table> tables) =>
        (_name, _features, _collections, _tables) = (name, features, collections, tables);

    public static CatalogBuilder Create(string name) =>
        new(name,
            ImmutableList<Feature>.Empty,
            ImmutableList<Collection>.Empty,
            ImmutableDictionary<string, Table>.Empty);

    private CatalogBuilder AddFeature(Feature feature) =>
        _features.Any(f => f.Name == feature.Name)
            ? throw new ArgumentException($"Feature with name '{feature.Name}' already exists.")
            : new CatalogBuilder(_name, _features.Add(feature), _collections, _tables);

    public CatalogBuilder Choice(string name, Func<ChoiceFeatureBuilder, ChoiceFeatureBuilder> setupFeature) =>
        AddFeature(setupFeature(ChoiceFeatureBuilder.Create(name)).Build());

    public CatalogBuilder Number(string name, Func<NumberFeatureBuilder, NumberFeatureBuilder> setupFeature) =>
        AddFeature(setupFeature(NumberFeatureBuilder.Create(name)).Build());

    public CatalogBuilder Boolean(string name) =>
        AddFeature(new BooleanFeature(name));

    public CatalogBuilder Text(string name) =>
        AddFeature(new TextFeature(name));

    public CatalogBuilder Computed(string name, Expression expression) =>
        AddFeature(new ComputedFeature(name, expression));

    public CatalogBuilder Collection(string name, Func<CollectionBuilder, CollectionBuilder> setupCollection) =>
        new(_name, _features, _collections.Add(setupCollection(CollectionBuilder.Create(name)).Build()), _tables);

    public CatalogBuilder Table(string name, Table table) =>
        new(_name, _features, _collections, _tables.Add(name, table));

    public Catalog Build() => new(_name, _features, _collections, _tables);
}

public sealed record Catalog(
    string Name,
    IReadOnlyList<Feature> Features,
    IReadOnlyList<Collection> Collections,
    IReadOnlyDictionary<string, Table> Tables);

public abstract record Feature(string Name);

public sealed record ChoiceFeature(string Name, IReadOnlyList<FeatureOption> Options) : Feature(Name);

public sealed class ChoiceFeatureBuilder
{
    private readonly string _name;
    private readonly IReadOnlyList<FeatureOption> _options;

    private ChoiceFeatureBuilder(string name, IReadOnlyList<FeatureOption> options) =>
        (_name, _options) = (name, options);

    public static ChoiceFeatureBuilder Create(string name) => new(name, []);

    public ChoiceFeatureBuilder Option(
        string id,
        string name,
        Func<FeatureOptionBuilder, FeatureOptionBuilder>? setupOption = null)
    {
        var builder = FeatureOptionBuilder.Create(id, name);
        if (setupOption is not null) builder = setupOption(builder);
        return new ChoiceFeatureBuilder(_name, [.. _options.Append(builder.Build())]);
    }

    public ChoiceFeature Build() => new(_name, _options);
}

public sealed record FeatureOption(
    string Id,
    string Name,
    IReadOnlyDictionary<string, Value> Attributes,
    string Symbol);

public sealed class FeatureOptionBuilder
{
    private readonly string _id;
    private readonly string _name;
    private readonly string? _symbol;
    private readonly ImmutableDictionary<string, Value> _attributes;

    private FeatureOptionBuilder(string id,
        string name,
        string? symbol,
        ImmutableDictionary<string, Value> attributes) =>
        (_id, _name, _symbol, _attributes) = (id, name, symbol, attributes);

    public static FeatureOptionBuilder Create(string id, string name) =>
        new(id, name, null, ImmutableDictionary<string, Value>.Empty);

    public FeatureOptionBuilder Attribute(string key, Value value) =>
        new(_id, _name, _symbol, _attributes.Add(key, value));

    public FeatureOptionBuilder Symbol(string symbol) => new(_id, _name, symbol, _attributes);

    public FeatureOption Build() => new(_id, _name, _attributes, _symbol ?? _id);
}

public sealed record NumberFeature(
    string Name,
    decimal? Step,
    decimal? Min,
    decimal? Max,
    string? Unit) : Feature(Name);

public sealed class NumberFeatureBuilder
{
    private readonly string _name;
    private readonly decimal? _step;
    private readonly decimal? _min;
    private readonly decimal? _max;
    private readonly string? _unit;

    private NumberFeatureBuilder(string name,
        decimal? step = null,
        decimal? min = null,
        decimal? max = null,
        string? unit = null) =>
        (_name, _step, _min, _max, _unit) = (name, step, min, max, unit);

    public static NumberFeatureBuilder Create(string name) => new(name);
    public NumberFeatureBuilder Step(decimal step) => new(_name, step, _min, _max, _unit);
    public NumberFeatureBuilder Min(decimal min) => new(_name, _step, min, _max, _unit);
    public NumberFeatureBuilder Max(decimal max) => new(_name, _step, _min, max, _unit);
    public NumberFeatureBuilder Unit(string unit) => new(_name, _step, _min, _max, unit);
    public NumberFeature Build() => new(_name, _step, _min, _max, _unit);
}

public sealed record BooleanFeature(string Name) : Feature(Name);

public sealed record TextFeature(string Name) : Feature(Name);

public sealed record ComputedFeature(string Name, Expression Expression) : Feature(Name);

public sealed record Collection(string Name, IReadOnlyList<Stage> Stages, CollectionMode Mode)
{
    public IReadOnlyList<string> Features => [.. Stages.SelectMany(s => s.Features)];
}

public sealed record Stage(string Name, IReadOnlyList<string> Features);

public enum CollectionMode
{
    Sequential,
    Independent
}

public sealed class CollectionBuilder
{
    private readonly string _name;
    private readonly IReadOnlyList<Stage> _stages;
    private readonly CollectionMode _mode;

    private CollectionBuilder(string name, IReadOnlyList<Stage> stages, CollectionMode mode) =>
        (_name, _stages, _mode) = (name, stages, mode);

    public static CollectionBuilder Create(string name) => new(name, [], CollectionMode.Sequential);

    public CollectionBuilder Stage(string stageName, string feature, params IEnumerable<string> features)
        => new(_name, [.. _stages.Append(new Stage(stageName, [.. features.Prepend(feature)]))], _mode);

    public CollectionBuilder Independent()
        => new(_name, _stages, CollectionMode.Independent);

    public Collection Build() => new(_name, _stages, _mode);
}
