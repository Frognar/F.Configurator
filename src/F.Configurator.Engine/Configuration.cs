using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public sealed record Configuration(
    Catalog.Catalog Catalog,
    Collection Collection,
    EquatableDictionary<string, Value> Values)
{
    private int CurrentStageIndex
    {
        get
        {
            var i = Collection.Stages.ToList().FindIndex(s => !s.Features.All(Values.ContainsKey));
            return i < 0 ? Collection.Stages.Count - 1 : i;
        }
    }

    public string CurrentStage => Collection.Stages[CurrentStageIndex].Name;

    public EquatableList<string> VisibleFeatures =>
        [.. Collection.Stages.Take(CurrentStageIndex + 1).SelectMany(s => s.Features)];

    public static Configuration Start(Catalog.Catalog catalog, string collectionName)
    {
        var collection = catalog.Collections.FirstOrDefault(c => c.Name == collectionName);
        return collection is null
            ? throw new ArgumentException($"Collection '{collectionName}' not found in catalog.")
            : new Configuration(catalog, collection, EquatableDictionary<string, Value>.Empty);
    }

    public Configuration Set(string featureName, Value value)
    {
        var feature = Catalog.Features.FirstOrDefault(f => f.Name == featureName);
        if (feature is null || !Collection.Features.Contains(featureName))
        {
            throw new ArgumentException($"Feature '{featureName}' is not in collection '{Collection.Name}'.");
        }

        if (!VisibleFeatures.Contains(featureName))
        {
            throw new InvalidOperationException($"Feature '{featureName}' cannot be set in stage '{CurrentStage}'.");
        }

        if (!Accepts(feature, value))
        {
            throw new ArgumentException($"Feature '{featureName}' does not accept {value}.");
        }

        if (feature is ChoiceFeature option && value is OptionValue selected &&
            option.Options.All(o => o.Id != selected.Id))
        {
            throw new ArgumentException($"Option '{selected.Id}' not found in feature '{featureName}'.");
        }

        return new Configuration(Catalog, Collection, Values.SetItem(featureName, value));
    }

    private static bool Accepts(Feature feature, Value value) =>
        feature switch
        {
            NumberFeature => value is NumberValue,
            ChoiceFeature => value is OptionValue,
            _ => false
        };

    public Value ValueOf(string featureName) => Values.TryGetValue(featureName, out var value) ? value : Value.Missing;
}
