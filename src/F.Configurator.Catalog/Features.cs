using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public abstract record Feature(string Name);

public sealed record ChoiceFeature(
    string Name,
    EquatableList<FeatureOption> Options,
    EquatableList<AttributeDeclaration> Attributes) : Feature(Name);

public sealed class ChoiceFeatureBuilder
{
    private readonly ChoiceFeature _feature;

    private ChoiceFeatureBuilder(ChoiceFeature feature) => _feature = feature;

    public static ChoiceFeatureBuilder Create(string name) =>
        new(new ChoiceFeature(name, EquatableList<FeatureOption>.Empty, EquatableList<AttributeDeclaration>.Empty));

    public ChoiceFeatureBuilder Option(
        string id,
        string name,
        Func<FeatureOptionBuilder, FeatureOptionBuilder>? setupOption = null)
    {
        var builder = FeatureOptionBuilder.Create(id, name);
        if (setupOption is not null) builder = setupOption(builder);
        return new ChoiceFeatureBuilder(_feature with { Options = _feature.Options.Add(builder.Build()) });
    }

    public ChoiceFeatureBuilder Attribute(string name, AttributeType type, Value? value = null) =>
        new(_feature with { Attributes = _feature.Attributes.Add(new AttributeDeclaration(name, type, value)) });

    public ChoiceFeature Build() => _feature;
}

public sealed record FeatureOption(
    string Id,
    string Name,
    EquatableDictionary<string, Value> Attributes,
    string Symbol);

public sealed class FeatureOptionBuilder
{
    private readonly FeatureOption _option;

    private FeatureOptionBuilder(FeatureOption option) => _option = option;

    public static FeatureOptionBuilder Create(string id, string name) =>
        new(new FeatureOption(id, name, EquatableDictionary<string, Value>.Empty, id));

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
