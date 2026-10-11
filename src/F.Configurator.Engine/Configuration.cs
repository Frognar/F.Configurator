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
            var i = _collection.Stages.ToList()
                .FindIndex(s => !s.Features.Except(HiddenFeatures).All(_values.ContainsKey));

            return i < 0 ? _collection.Stages.Count - 1 : i;
        }
    }

    public string CurrentStage => _collection.Stages[CurrentStageIndex].Name;

    private IEnumerable<Rule> ActiveRules =>
        _catalog.Rules.Where(r =>
            (r.Collections.Count == 0 || r.Collections.Contains(_collection.Name)) &&
            (r.Condition is null || r.Condition.Evaluate(_values) is BooleanValue { IsTrue: true }));

    private IEnumerable<string> HiddenFeatures =>
        ActiveRules.SelectMany(r => r.Effects.OfType<HideEffect>()).SelectMany(e => e.Features)
            .Concat(ActiveRules.SelectMany(r => r.Effects.OfType<OnlyFromTableEffect>())
                .Where(e => OptionsFromTable(e) is null).Select(e => e.Feature));

    public EquatableList<string> VisibleFeatures =>
        [.. _collection.Stages.Take(CurrentStageIndex + 1).SelectMany(s => s.Features).Except(HiddenFeatures)];

    public EquatableList<string> LockedFeatures =>
        [.. ActiveRules.SelectMany(r => r.Effects.OfType<LockEffect>().SelectMany(e => e.Features))];

    public static Configuration Start(ProductCatalog catalog, string collectionName)
    {
        var collection = catalog.Collections.FirstOrDefault(c => c.Name == collectionName);
        return collection is null
            ? throw new ArgumentException($"Collection '{collectionName}' not found in catalog.")
            : new Configuration(catalog, collection, EquatableDictionary<string, Value>.Empty);
    }

    public EquatableList<string> AvailableOptions(string featureName) =>
    [
        .. ChoiceFeature(featureName).Options.Select(o => o.Id)
            .Except(UnavailableOptions(featureName).Select(u => u.Id))
    ];

    public EquatableList<UnavailableOption> UnavailableOptions(string featureName) =>
    [
        .. ChoiceFeature(featureName).Options
            .Select(o => (o.Id, Rule: ExcludingRule(featureName, Value.Option(o.Id))))
            .Where(x => x.Rule is not null)
            .Select(x => new UnavailableOption(x.Id, x.Rule!.Name))
    ];

    private ChoiceFeature ChoiceFeature(string featureName) =>
        _catalog.Features.OfType<ChoiceFeature>().FirstOrDefault(f => f.Name == featureName)
        ?? throw new ArgumentException($"Feature '{featureName}' is not a choice feature in catalog.");

    private Rule? ExcludingRule(string featureName, Value option) =>
        ActiveRules.FirstOrDefault(r => r.Effects.Any(e => e switch
        {
            OnlyEffect only => only.Feature == featureName && !only.Options.Contains(option),
            ForbidEffect forbid => forbid.Feature == featureName && forbid.Options.Contains(option),
            OnlyFromTableEffect onlyFromTable => onlyFromTable.Feature == featureName && !(OptionsFromTable(onlyFromTable)?.Contains(option) ?? true),
            _ => false,
        }));

    private EquatableList<Value>? OptionsFromTable(OnlyFromTableEffect onlyFromTable)
    {
        var table = _catalog.AllowedCombinations[onlyFromTable.Table];
        var keyValues = EquatableList.Create(table.KeyColumns
            .Select(k => _values.TryGetValue(k, out var value) ? value : Value.Missing));

        return table.Rows.GetValueOrDefault(keyValues);
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

        if (LockedFeatures.Contains(featureName))
        {
            throw new InvalidOperationException($"Feature '{featureName}' is locked.");
        }

        if (value is OptionValue option && UnavailableOptions(featureName).Any(o => o.Id == option.Id))
        {
            throw new InvalidOperationException($"Option '{option.Id}' is unavailable in feature '{featureName}'.");
        }

        if (!Accepts(feature, value))
        {
            throw new ArgumentException($"Feature '{featureName}' does not accept {value}.");
        }

        var newConfiguration = new Configuration(_catalog, _collection, _values.SetItem(featureName, value));
        return new Configuration(
            _catalog,
            _collection,
            newConfiguration._values
                .Where(kvp => !newConfiguration.HiddenFeatures.Contains(kvp.Key))
                .ToEquatableDictionary());
    }

    private static bool Accepts(Feature feature, Value value) =>
        (feature, value) switch
        {
            (NumberFeature, NumberValue) => true,
            (ChoiceFeature choice, OptionValue selected) => choice.Options.Any(o => o.Id == selected.Id),
            _ => false,
        };
}

public sealed record UnavailableOption(string Id, string RuleName);
