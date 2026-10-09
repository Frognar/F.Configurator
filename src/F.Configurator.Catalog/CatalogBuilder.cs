using System.Collections.Immutable;
using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public sealed class CatalogBuilder
{
    private readonly Catalog _catalog;

    private CatalogBuilder(Catalog catalog) => _catalog = catalog;

    public static CatalogBuilder Create(string name) =>
        new(new Catalog(name,
            ImmutableList<Feature>.Empty,
            ImmutableList<Collection>.Empty,
            ImmutableList<Rule>.Empty,
            ImmutableDictionary<string, Table>.Empty));

    private CatalogBuilder AddFeature(Feature feature) =>
        _catalog.Features.Any(f => f.Name == feature.Name)
            ? throw new ArgumentException($"Feature with name '{feature.Name}' already exists.")
            : new CatalogBuilder(_catalog with { Features = _catalog.Features.Add(feature) });

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
        new(_catalog with
        {
            Collections = _catalog.Collections.Add(setupCollection(CollectionBuilder.Create(name)).Build())
        });

    public CatalogBuilder Rule(string name, Func<RuleBuilder, RuleBuilder> setupRule) =>
        new(_catalog with
        {
            Rules = _catalog.Rules.Add(setupRule(RuleBuilder.Create(name)).Build())
        });

    public CatalogBuilder Table(string name, Table table) =>
        new(_catalog with { Tables = _catalog.Tables.Add(name, table) });

    public Catalog Build() => _catalog;
}

public sealed record Catalog(
    string Name,
    ImmutableList<Feature> Features,
    ImmutableList<Collection> Collections,
    ImmutableList<Rule> Rules,
    ImmutableDictionary<string, Table> Tables);

public abstract record Feature(string Name);

public sealed record ChoiceFeature(string Name, ImmutableList<FeatureOption> Options) : Feature(Name);

public sealed class ChoiceFeatureBuilder
{
    private readonly ChoiceFeature _feature;

    private ChoiceFeatureBuilder(ChoiceFeature feature) => _feature = feature;

    public static ChoiceFeatureBuilder Create(string name) => new(new ChoiceFeature(name, []));

    public ChoiceFeatureBuilder Option(
        string id,
        string name,
        Func<FeatureOptionBuilder, FeatureOptionBuilder>? setupOption = null)
    {
        var builder = FeatureOptionBuilder.Create(id, name);
        if (setupOption is not null) builder = setupOption(builder);
        return new ChoiceFeatureBuilder(_feature with { Options = _feature.Options.Add(builder.Build()) });
    }

    public ChoiceFeature Build() => _feature;
}

public sealed record FeatureOption(
    string Id,
    string Name,
    ImmutableDictionary<string, Value> Attributes,
    string Symbol);

public sealed class FeatureOptionBuilder
{
    private readonly FeatureOption _option;

    private FeatureOptionBuilder(FeatureOption option) => _option = option;

    public static FeatureOptionBuilder Create(string id, string name) =>
        new(new FeatureOption(id, name, ImmutableDictionary<string, Value>.Empty, id));

    public FeatureOptionBuilder Attribute(string key, Value value) =>
        new(_option with { Attributes = _option.Attributes.Add(key, value) });

    public FeatureOptionBuilder Symbol(string symbol) => new(_option with { Symbol = symbol });

    public FeatureOption Build() => _option;
}

public sealed record NumberFeature(
    string Name,
    decimal? Step,
    decimal? Min,
    decimal? Max,
    string? Unit) : Feature(Name);

public sealed class NumberFeatureBuilder
{
    private readonly NumberFeature _feature;

    private NumberFeatureBuilder(NumberFeature feature) => _feature = feature;

    public static NumberFeatureBuilder Create(string name) =>
        new(new NumberFeature(name, null, null, null, null));

    public NumberFeatureBuilder Step(decimal step) => new(_feature with { Step = step });
    public NumberFeatureBuilder Min(decimal min) => new(_feature with { Min = min });
    public NumberFeatureBuilder Max(decimal max) => new(_feature with { Max = max });
    public NumberFeatureBuilder Unit(string unit) => new(_feature with { Unit = unit });
    public NumberFeature Build() => _feature;
}

public sealed record BooleanFeature(string Name) : Feature(Name);

public sealed record TextFeature(string Name) : Feature(Name);

public sealed record ComputedFeature(string Name, Expression Expression) : Feature(Name);

public sealed record Collection(string Name, ImmutableList<Stage> Stages, CollectionMode Mode)
{
    public ImmutableList<string> Features => [.. Stages.SelectMany(s => s.Features)];
}

public sealed record Stage(string Name, ImmutableList<string> Features);

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
        new(new Collection(name, ImmutableList<Stage>.Empty, CollectionMode.Sequential));

    public CollectionBuilder Stage(string stageName, string feature, params IEnumerable<string> features)
        => new(_collection with
        {
            Stages = _collection.Stages.Add(new Stage(stageName, [.. features.Prepend(feature)]))
        });

    public CollectionBuilder Independent()
        => new(_collection with { Mode = CollectionMode.Independent });

    public Collection Build() => _collection;
}

public sealed record Rule(string Name, int Priority, ImmutableList<string> Collections, Expression? Condition);

public sealed class RuleBuilder
{
    private readonly Rule _rule;

    private RuleBuilder(Rule rule) => _rule = rule;

    public static RuleBuilder Create(string name) =>
        new(new Rule(name, 0, ImmutableList<string>.Empty, null));

    public RuleBuilder When(Expression condition) => new(_rule with { Condition = condition });

    public RuleBuilder Priority(int priority) => new(_rule with { Priority = priority });

    public RuleBuilder AppliesTo(string collection, params IEnumerable<string> collections) =>
        new(_rule with { Collections = _rule.Collections.Add(collection).AddRange(collections) });

    public RuleBuilder Hide(string feature) => this;

    public RuleBuilder Max(string feature, Expression limit) => this;

    public Rule Build() => _rule;
}
