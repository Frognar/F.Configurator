using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public sealed record Configuration(Catalog.Catalog Catalog, string CollectionName)
{
    public string CurrentStage => "Model";

    public EquatableList<string> VisibleFeatures =>
        Catalog.Collections.First(c => c.Name == CollectionName)
            .Stages.First(s => s.Name == CurrentStage)
            .Features;

    public static Configuration Start(Catalog.Catalog catalog, string collectionName)
    {
        return new Configuration(catalog, collectionName);
    }

    public Value ValueOf(string featureName) => Value.Missing;
}
