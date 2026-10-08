using System.Collections.Immutable;
using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public record CatalogBuilder(string CatalogName, IReadOnlyList<Feature> Features)
{
    public static CatalogBuilder Create(string name) => new(name, []);

    public CatalogBuilder Choice(string name, Func<ChoiceFeatureBuilder, ChoiceFeatureBuilder> setupFeature) =>
        AddFeature(setupFeature(ChoiceFeatureBuilder.Create(name)).Build());

    public CatalogBuilder Number(string name, Func<NumberFeatureBuilder, NumberFeatureBuilder> setupFeature) =>
        AddFeature(setupFeature(NumberFeatureBuilder.Create(name)).Build());

    public CatalogBuilder Boolean(string name) => AddFeature(BooleanFeatureBuilder.Create(name).Build());

    public CatalogBuilder Text(string name) => AddFeature(TextFeatureBuilder.Create(name).Build());

    private CatalogBuilder AddFeature(Feature feature) => this with { Features = [.. Features.Append(feature)] };

    public Catalog Build() => new(CatalogName, Features);
}

public sealed record Catalog(string Name, IReadOnlyList<Feature> Features);

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
    ImmutableDictionary<string, Value> Attributes,
    string? SymbolValue = null)
{
    public static FeatureOptionBuilder Create(string id, string name) =>
        new(id, name, ImmutableDictionary<string, Value>.Empty);

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

public sealed record BooleanFeatureBuilder(string Name)
{
    public static BooleanFeatureBuilder Create(string name) => new(name);
    public BooleanFeature Build() => new(Name);
}

public sealed record TextFeature(string Name) : Feature(Name);

public sealed record TextFeatureBuilder(string Name)
{
    public static TextFeatureBuilder Create(string name) => new(name);
    public TextFeature Build() => new(Name);
}
