using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public sealed class Configuration
{
    private readonly ProductCatalog _catalog;
    private readonly Collection _collection;
    private readonly EquatableDictionary<string, Value> _values;

    private Configuration(ProductCatalog catalog, Collection collection, EquatableDictionary<string, Value> values) =>
        (_catalog, _collection, _values) = (catalog, collection, values);

    private int CurrentStageIndex
    {
        get
        {
            var i = _collection.Stages.ToList().FindIndex(s => !s.Features.All(_values.ContainsKey));
            return i < 0 ? _collection.Stages.Count - 1 : i;
        }
    }

    public string CurrentStage => _collection.Stages[CurrentStageIndex].Name;

    public EquatableList<string> VisibleFeatures =>
    [
        .. _collection.Stages.Take(CurrentStageIndex + 1).SelectMany(s => s.Features)
            .Except(_catalog.Rules.Where(r =>
                    (!r.Collections.Any() || r.Collections.Contains(_collection.Name)) &&
                    (r.Condition is null ||
                     r.Condition.Evaluate(_values) is BooleanValue { IsTrue: true }))
                .SelectMany(r => r.Effects.OfType<HideEffect>().SelectMany(e => e.Features)))
    ];

    public static Configuration Start(ProductCatalog catalog, string collectionName)
    {
        var collection = catalog.Collections.FirstOrDefault(c => c.Name == collectionName);
        return collection is null
            ? throw new ArgumentException($"Collection '{collectionName}' not found in catalog.")
            : new Configuration(catalog, collection, EquatableDictionary<string, Value>.Empty);
    }

    public Value ValueOf(string featureName) => _values.TryGetValue(featureName, out var value) ? value : Value.Missing;

    public Configuration Set(string featureName, Value value)
    {
        var feature = _catalog.Features.FirstOrDefault(f => f.Name == featureName);
        if (feature is null || !_collection.Features.Contains(featureName))
        {
            throw new ArgumentException($"Feature '{featureName}' is not in collection '{_collection.Name}'.");
        }

        if (!VisibleFeatures.Contains(featureName))
        {
            throw new InvalidOperationException($"Feature '{featureName}' cannot be set in stage '{CurrentStage}'.");
        }

        if (!Accepts(feature, value))
        {
            throw new ArgumentException($"Feature '{featureName}' does not accept {value}.");
        }

        return new Configuration(_catalog, _collection, _values.SetItem(featureName, value));
    }

    private static bool Accepts(Feature feature, Value value) =>
        (feature, value) switch
        {
            (NumberFeature, NumberValue) => true,
            (ChoiceFeature choice, OptionValue selected) => choice.Options.Any(o => o.Id == selected.Id),
            _ => false,
        };
}
