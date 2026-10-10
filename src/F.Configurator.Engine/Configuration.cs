using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public sealed class Configuration(
    Catalog.Catalog catalog,
    Collection collection,
    EquatableDictionary<string, Value> values)
{
    private int CurrentStageIndex
    {
        get
        {
            var i = collection.Stages.ToList().FindIndex(s => !s.Features.All(values.ContainsKey));
            return i < 0 ? collection.Stages.Count - 1 : i;
        }
    }

    public string CurrentStage => collection.Stages[CurrentStageIndex].Name;

    public EquatableList<string> VisibleFeatures =>
        [.. collection.Stages.Take(CurrentStageIndex + 1).SelectMany(s => s.Features)];

    public static Configuration Start(Catalog.Catalog catalog, string collectionName)
    {
        var collection = catalog.Collections.FirstOrDefault(c => c.Name == collectionName);
        return collection is null
            ? throw new ArgumentException($"Collection '{collectionName}' not found in catalog.")
            : new Configuration(catalog, collection, EquatableDictionary<string, Value>.Empty);
    }

    public Configuration Set(string featureName, Value value)
    {
        var feature = catalog.Features.FirstOrDefault(f => f.Name == featureName);
        if (feature is null || !collection.Features.Contains(featureName))
        {
            throw new ArgumentException($"Feature '{featureName}' is not in collection '{collection.Name}'.");
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

        return new Configuration(catalog, collection, values.SetItem(featureName, value));
    }

    private static bool Accepts(Feature feature, Value value) =>
        feature switch
        {
            NumberFeature => value is NumberValue,
            ChoiceFeature => value is OptionValue,
            _ => false
        };

    public Value ValueOf(string featureName) => values.TryGetValue(featureName, out var value) ? value : Value.Missing;
}
