using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public sealed record Configuration(Collection Collection)
{
    private EquatableDictionary<string, Value> Values { get; init; } = EquatableDictionary<string, Value>.Empty;

    public string CurrentStage =>
        (Collection.Stages.FirstOrDefault(stage => !stage.Features.All(Values.ContainsKey))
         ?? Collection.Stages[^1]).Name;

    public EquatableList<string> VisibleFeatures => [.. StagesUpToCurrent.SelectMany(s => s.Features)];

    private IEnumerable<Stage> StagesUpToCurrent
    {
        get
        {
            foreach (var stage in Collection.Stages)
            {
                yield return stage;

                if (stage.Name == CurrentStage)
                {
                    yield break;
                }
            }
        }
    }

    public static Configuration Start(Catalog.Catalog catalog, string collectionName)
    {
        var collection = catalog.Collections.FirstOrDefault(c => c.Name == collectionName);
        return collection is null
            ? throw new ArgumentException($"Collection '{collectionName}' not found in catalog.")
            : new Configuration(collection);
    }

    public Value ValueOf(string featureName) => Values.TryGetValue(featureName, out var value) ? value : Value.Missing;

    public Configuration Set(string featureName, Value value) =>
        Collection.Features.Contains(featureName)
            ? VisibleFeatures.Contains(featureName)
                ? this with { Values = Values.SetItem(featureName, value) }
                : throw new InvalidOperationException(
                    $"Feature '{featureName}' cannot be set in stage '{CurrentStage}'.")
            : throw new ArgumentException($"Feature '{featureName}' not found in catalog.");
}
