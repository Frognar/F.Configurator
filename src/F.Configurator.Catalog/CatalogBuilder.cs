namespace F.Configurator.Catalog;

public record CatalogBuilder(IReadOnlyList<ChoiceFeature> Features)
{
    public static CatalogBuilder Create(string name) => new([]);

    public CatalogBuilder Choice(string name, Func<ChoiceFeature, ChoiceFeature> setupFeature)
    {
        ChoiceFeature feature = new(name, []);
        feature = setupFeature(feature);
        return new CatalogBuilder(Features: [.. Features.Append(feature)]);
    }

    public Catalog Build() => new(Features);
}

public sealed record Catalog(IReadOnlyList<ChoiceFeature> Features);

public sealed record ChoiceFeature(string Name, IReadOnlyList<FeatureOption> Options)
{
    public ChoiceFeature Option(string id, string name)
    {
        return this with { Options = [.. Options.Append(new FeatureOption(id))] };
    }
}

public sealed record FeatureOption(string Id);
