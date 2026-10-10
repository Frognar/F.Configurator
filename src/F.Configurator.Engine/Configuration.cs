using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public sealed record Configuration(Catalog.Catalog Catalog, string CollectionName)
{
    public string CurrentStage => "Model";
    private EquatableDictionary<string, Value> Values { get; init; } = EquatableDictionary<string, Value>.Empty;

    public EquatableList<string> VisibleFeatures =>
        Catalog.Collections.First(c => c.Name == CollectionName)
            .Stages.First(s => s.Name == CurrentStage)
            .Features;

    public static Configuration Start(Catalog.Catalog catalog, string collectionName)
    {
        var collection = catalog.Collections.FirstOrDefault(c => c.Name == collectionName);
        return collection is null
            ? throw new ArgumentException($"Collection '{collectionName}' not found in catalog.")
            : new Configuration(catalog, collection.Name);
    }

    public Value ValueOf(string featureName) => Values.TryGetValue(featureName, out var value) ? value : Value.Missing;

    public Configuration Set(string featureName, Value value) =>
        this with { Values = Values.SetItem(featureName, value) };
}
