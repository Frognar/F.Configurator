using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public abstract record Feature(string Name)
{
    public bool IsRequired { get; init; } = true;

    public FeatureMetadata Metadata { get; init; } = FeatureMetadata.None;
}

// What the wizard shows; the engine does not use it.
public sealed record FeatureMetadata(string? Label, string? Description, Presentation? Presentation, string? Image)
{
    public static FeatureMetadata None { get; } = new(null, null, null, null);
}

public enum Presentation
{
    List,
    Buttons,
    Tiles,
    Swatches,
    Field,
    Slider,
}

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

    public ChoiceFeatureBuilder Optional() => new(_feature with { IsRequired = false });

    public ChoiceFeatureBuilder Metadata(FeatureMetadata metadata) => new(_feature with { Metadata = metadata });

    public ChoiceFeature Build() => _feature;
}

public sealed record FeatureOption(
    string Id,
    string Name,
    string Symbol,
    EquatableDictionary<string, Value> Attributes)
{
    public string? Image { get; init; }

    // The code of the option in another system, such as the ERP (G11).
    public string? ExternalCode { get; init; }
}

public sealed class FeatureOptionBuilder
{
    private readonly FeatureOption _option;

    private FeatureOptionBuilder(FeatureOption option) => _option = option;

    public static FeatureOptionBuilder Create(string id, string name) =>
        new(new FeatureOption(id, name, id, EquatableDictionary<string, Value>.Empty));

    public FeatureOptionBuilder Attribute(string key, Value value) =>
        new(_option with { Attributes = _option.Attributes.Add(key, value) });

    public FeatureOptionBuilder Symbol(string symbol) => new(_option with { Symbol = symbol });

    public FeatureOptionBuilder Image(string image) => new(_option with { Image = image });

    public FeatureOptionBuilder ExternalCode(string code) => new(_option with { ExternalCode = code });

    public FeatureOption Build() => _option;
}

public sealed record NumberFeature(
    string Name,
    string? Unit,
    decimal? Min,
    decimal? Max,
    decimal? Step) : Feature(Name);

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
    public NumberFeatureBuilder Optional() => new(_feature with { IsRequired = false });
    public NumberFeatureBuilder Metadata(FeatureMetadata metadata) => new(_feature with { Metadata = metadata });
    public NumberFeature Build() => _feature;
}

public sealed record BooleanFeature(string Name) : Feature(Name);

public sealed record TextFeature(string Name, int? MinLength, int? MaxLength) : Feature(Name);

public sealed class TextFeatureBuilder
{
    private readonly TextFeature _feature;

    private TextFeatureBuilder(TextFeature feature) => _feature = feature;

    public static TextFeatureBuilder Create(string name) =>
        new(new TextFeature(name, null, null));

    public TextFeatureBuilder Length(int min, int max) => new(_feature with { MinLength = min, MaxLength = max });
    public TextFeatureBuilder Optional() => new(_feature with { IsRequired = false });
    public TextFeatureBuilder Metadata(FeatureMetadata metadata) => new(_feature with { Metadata = metadata });
    public TextFeature Build() => _feature;
}

public sealed record ComputedFeature(string Name, Expression Expression) : Feature(Name);
