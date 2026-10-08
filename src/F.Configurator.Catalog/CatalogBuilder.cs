namespace F.Configurator.Catalog;

public record CatalogBuilder(string CatalogName, IReadOnlyList<ChoiceFeature> Features)
{
    public static CatalogBuilder Create(string name) => new(name, []);

    public CatalogBuilder Choice(string name, Func<ChoiceFeature, ChoiceFeature> setupFeature)
    {
        ChoiceFeature feature = new(name, []);
        feature = setupFeature(feature);
        return this with { Features = [.. Features.Append(feature)] };
    }

    public Catalog Build() => new(CatalogName, Features);
}

public sealed record Catalog(string Name, IReadOnlyList<ChoiceFeature> Features);

public sealed record ChoiceFeature(string Name, IReadOnlyList<FeatureOption> Options)
{
    public ChoiceFeature Option(string id, string name)
    {
        return this with { Options = [.. Options.Append(new FeatureOption(id, name))] };
    }
}

public sealed record FeatureOption(string Id, string Name, string? Symbol = null)
{
    public string Symbol { get; } = Symbol ?? Id;
}
