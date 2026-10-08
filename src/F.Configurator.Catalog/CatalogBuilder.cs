namespace F.Configurator.Catalog;

public record CatalogBuilder(string CatalogName, IReadOnlyList<Feature> Features)
{
    public static CatalogBuilder Create(string name) => new(name, []);

    public CatalogBuilder Choice(string name, Func<ChoiceFeature, ChoiceFeature> setupFeature)
    {
        ChoiceFeature feature = new(name, []);
        feature = setupFeature(feature);
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

public sealed record ChoiceFeature(string Name, IReadOnlyList<FeatureOption> Options) : Feature(Name)
{
    public ChoiceFeature Option(string id, string name, string? symbol = null)
    {
        return this with { Options = [.. Options.Append(new FeatureOption(id, name, symbol))] };
    }
}

public sealed record FeatureOption(string Id, string Name, string? Symbol = null)
{
    public string Symbol { get; } = Symbol ?? Id;
}

public sealed record NumberFeatureBuilder(string Name, string UnitName)
{
    public static NumberFeatureBuilder Create(string name) => new(name, string.Empty);
    public NumberFeatureBuilder Unit(string unit) => this with { UnitName = unit };
    public NumberFeature Build() => new(Name, UnitName);
}

public sealed record NumberFeature(string Name, string Unit) : Feature(Name);
