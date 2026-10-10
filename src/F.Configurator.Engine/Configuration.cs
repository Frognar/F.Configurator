using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public sealed record Configuration(Collection Collection)
{
    private EquatableDictionary<string, Value> Values { get; init; } = EquatableDictionary<string, Value>.Empty;
    public string CurrentStage => "Model";

    public EquatableList<string> VisibleFeatures =>
        Collection
            .Stages.First(s => s.Name == CurrentStage)
            .Features;

    public static Configuration Start(Catalog.Catalog catalog, string collectionName)
    {
        var collection = catalog.Collections.FirstOrDefault(c => c.Name == collectionName);
        return collection is null
            ? throw new ArgumentException($"Collection '{collectionName}' not found in catalog.")
            : new Configuration(collection);
    }

    public Value ValueOf(string featureName) => Values.TryGetValue(featureName, out var value) ? value : Value.Missing;

    public Configuration Set(string featureName, Value value) =>
        this with { Values = Values.SetItem(featureName, value) };
}
