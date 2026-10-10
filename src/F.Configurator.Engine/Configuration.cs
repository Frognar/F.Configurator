using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public sealed record Configuration(Catalog.Catalog Catalog, Collection Collection)
{
    private EquatableDictionary<string, Value> Values { get; init; } = EquatableDictionary<string, Value>.Empty;

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
            : new Configuration(catalog, collection);
    }

    public Configuration Set(string featureName, Value value) =>
        Collection.Features.Contains(featureName)
            ? VisibleFeatures.Contains(featureName)
                ? (Catalog.Features.First(f => f.Name == featureName), value) switch
                {
                    (ChoiceFeature choice, OptionValue selected)
                        when choice.Options.All(o => o.Id != selected.Id) =>
                        throw new ArgumentException($"Option '{selected.Id}' not found in feature '{featureName}'."),
                    var (feature, _) when !MatchType(feature, value) =>
                        throw new ArgumentException($"Value '{value}' is not of type '{feature.GetType()}'"),
                    _ => this with { Values = Values.SetItem(featureName, value) }
                }
                : throw new InvalidOperationException(
                    $"Feature '{featureName}' cannot be set in stage '{CurrentStage}'.")
            : throw new ArgumentException($"Feature '{featureName}' not found in catalog.");

    private static bool MatchType(Feature feature, Value value) =>
        feature switch
        {
            NumberFeature => value is NumberValue,
            ChoiceFeature => value is OptionValue,
            _ => false
        };

    public Value ValueOf(string featureName) => Values.TryGetValue(featureName, out var value) ? value : Value.Missing;
}
