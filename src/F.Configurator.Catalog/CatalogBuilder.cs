namespace F.Configurator.Catalog;

public record CatalogBuilder(string CatalogName, IReadOnlyList<Feature> Features)
{
    public static CatalogBuilder Create(string name) => new(name, []);

    public CatalogBuilder Choice(string name, Func<ChoiceFeatureBuilder, ChoiceFeatureBuilder> setupFeature)
    {
        var feature = setupFeature(ChoiceFeatureBuilder.Create(name)).Build();
        return this with { Features = [.. Features.Append(feature)] };
    }

    public CatalogBuilder Number(string name, Func<NumberFeatureBuilder, NumberFeatureBuilder> setupFeature)
    {
        var feature = setupFeature(NumberFeatureBuilder.Create(name)).Build();
        return this with { Features = [.. Features.Append(feature)] };
    }

    public Catalog Build() => new(CatalogName, Features);
}

public sealed record Catalog(string Name, IReadOnlyList<Feature> Features);

public abstract record Feature(string Name);

public sealed record ChoiceFeature(string Name, IReadOnlyList<FeatureOption> Options) : Feature(Name);

public sealed record FeatureOption(string Id, string Name, string Symbol);

public sealed record ChoiceFeatureBuilder(string Name, IReadOnlyList<FeatureOption> Options)
{
    public static ChoiceFeatureBuilder Create(string name) => new(name, []);

    public ChoiceFeatureBuilder Option(string id, string name, string? symbol = null) =>
        this with { Options = [.. Options.Append(new FeatureOption(id, name, symbol ?? id))] };

    public ChoiceFeature Build() => new(Name, Options);
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
