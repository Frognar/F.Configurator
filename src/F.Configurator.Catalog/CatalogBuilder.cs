using System.Collections.Immutable;
using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public record CatalogBuilder(
    string CatalogName,
    ImmutableList<Feature> Features,
    ImmutableList<Collection> Collections,
    ImmutableDictionary<string, Table> Tables)
{
    public static CatalogBuilder Create(string name) =>
        new(name,
            ImmutableList<Feature>.Empty,
            ImmutableList<Collection>.Empty,
            ImmutableDictionary<string, Table>.Empty);

    public CatalogBuilder Choice(string name, Func<ChoiceFeatureBuilder, ChoiceFeatureBuilder> setupFeature) =>
        AddFeature(setupFeature(ChoiceFeatureBuilder.Create(name)).Build());

    public CatalogBuilder Number(string name, Func<NumberFeatureBuilder, NumberFeatureBuilder> setupFeature) =>
        AddFeature(setupFeature(NumberFeatureBuilder.Create(name)).Build());

    public CatalogBuilder Boolean(string name) => AddFeature(new BooleanFeature(name));

    public CatalogBuilder Text(string name) => AddFeature(new TextFeature(name));

    public CatalogBuilder Computed(string name, Expression expression) =>
        AddFeature(new ComputedFeature(name, expression));

    public CatalogBuilder Table(string name, Table table) => this with { Tables = Tables.Add(name, table) };

    public CatalogBuilder Collection(string name, Func<CollectionBuilder, CollectionBuilder> setupCollection) =>
        this with { Collections = Collections.Add(setupCollection(CollectionBuilder.Create(name)).Build()) };

    private CatalogBuilder AddFeature(Feature feature)
    {
        if (Features.Any(f => f.Name == feature.Name))
        {
            throw new ArgumentException($"Feature with name '{feature.Name}' already exists.");
        }

        return this with { Features = Features.Add(feature) };
    }

    public Catalog Build() => new(CatalogName, Features, Collections, Tables);
}

public sealed record Catalog(
    string Name,
    IReadOnlyList<Feature> Features,
    IReadOnlyList<Collection> Collections,
    IReadOnlyDictionary<string, Table> Tables);

public enum CollectionMode
{
    Sequential,
    Independent
}

public sealed record Collection(string Name, IReadOnlyList<Stage> Stages, CollectionMode Mode)
{
    public IReadOnlyList<string> Features => [.. Stages.SelectMany(s => s.Features)];
}

public sealed record CollectionBuilder(string Name, IReadOnlyList<Stage> Stages, CollectionMode Mode)
{
    public static CollectionBuilder Create(string name) => new(name, [], CollectionMode.Sequential);

    public CollectionBuilder Stage(string stageName, string feature, params IEnumerable<string> features)
        => this with { Stages = [.. Stages.Append(new Stage(stageName, [.. features.Prepend(feature)]))] };

    public CollectionBuilder Independent()
        => this with { Mode = CollectionMode.Independent };

    public Collection Build() => new(Name, Stages, Mode);
}

public sealed record Stage(string Name, IReadOnlyList<string> Features);

public abstract record Feature(string Name);

public sealed record ChoiceFeature(string Name, IReadOnlyList<FeatureOption> Options) : Feature(Name);

public sealed record ChoiceFeatureBuilder(string Name, IReadOnlyList<FeatureOption> Options)
{
    public static ChoiceFeatureBuilder Create(string name) => new(name, []);

    public ChoiceFeatureBuilder Option(
        string id,
        string name,
        Func<FeatureOptionBuilder, FeatureOptionBuilder>? setupOption = null)
    {
        var builder = FeatureOptionBuilder.Create(id, name);
        if (setupOption is not null) builder = setupOption(builder);
        return this with { Options = [.. Options.Append(builder.Build())] };
    }

    public ChoiceFeature Build() => new(Name, Options);
}

public sealed record FeatureOption(
    string Id,
    string Name,
    IReadOnlyDictionary<string, Value> Attributes,
    string Symbol);

public sealed record FeatureOptionBuilder(
    string Id,
    string Name,
    string? SymbolValue,
    ImmutableDictionary<string, Value> Attributes)
{
    public static FeatureOptionBuilder Create(string id, string name) =>
        new(id, name, null, ImmutableDictionary<string, Value>.Empty);

    public FeatureOptionBuilder Attribute(string key, Value value) =>
        this with { Attributes = Attributes.Add(key, value) };

    public FeatureOptionBuilder Symbol(string symbol) => this with { SymbolValue = symbol };

    public FeatureOption Build() => new(Id, Name, Attributes, SymbolValue ?? Id);
}

public sealed record NumberFeature(
    string Name,
    decimal? Step,
    decimal? Min,
    decimal? Max,
    string? Unit) : Feature(Name);

public sealed record NumberFeatureBuilder(
    string Name,
    decimal? StepValue = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    string? UnitName = null)
{
    public static NumberFeatureBuilder Create(string name) => new(name);
    public NumberFeatureBuilder Unit(string unit) => this with { UnitName = unit };
    public NumberFeatureBuilder Step(decimal step) => this with { StepValue = step };
    public NumberFeatureBuilder Min(decimal min) => this with { MinValue = min };
    public NumberFeatureBuilder Max(decimal max) => this with { MaxValue = max };
    public NumberFeature Build() => new(Name, StepValue, MinValue, MaxValue, UnitName);
}

public sealed record BooleanFeature(string Name) : Feature(Name);

public sealed record TextFeature(string Name) : Feature(Name);

public sealed record ComputedFeature(string Name, Expression Expression) : Feature(Name);
