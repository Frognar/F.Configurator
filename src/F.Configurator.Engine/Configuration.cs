using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public sealed class Configuration
{
    private readonly ProductCatalog _catalog;
    private readonly Collection _collection;
    private readonly EquatableDictionary<string, Value> _userValues;
    private readonly EquatableDictionary<string, Value> _forcedValues;
    private readonly EquatableDictionary<string, Value> _defaultValues;
    private readonly EquatableDictionary<string, Value> _values;
    private readonly EquatableList<Rule> _activeRules;

    private Configuration(
        ProductCatalog catalog,
        Collection collection,
        EquatableDictionary<string, Value> userValues,
        EquatableDictionary<string, Value> forcedValues,
        EquatableDictionary<string, Value> defaultValues)
    {
        _catalog = catalog;
        _collection = collection;
        _userValues = userValues;
        _forcedValues = forcedValues;
        _defaultValues = defaultValues;
        _values = userValues.Concat(forcedValues)
            .Aggregate(defaultValues, (values, kvp) => values.SetItem(kvp.Key, kvp.Value));

        _activeRules =
        [
            .. _catalog.Rules.Where(r =>
                (r.Collections.Count == 0 || r.Collections.Contains(_collection.Name)) &&
                (r.Condition is null || r.Condition.Evaluate(_values) is BooleanValue { IsTrue: true }))
        ];
    }

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

    private IEnumerable<(Rule Rule, T Effect)> Effects<T>() where T : Effect =>
        _activeRules.SelectMany(r => r.Effects.OfType<T>().Select(e => (r, e)));

    private IEnumerable<string> HiddenFeatures =>
        Effects<HideEffect>().SelectMany(e => e.Effect.Features)
            .Concat(Effects<OnlyFromTableEffect>()
                .Where(x => OptionsFromTable(x.Effect) is { Count: 0 })
                .Select(x => x.Effect.Feature));

    public EquatableList<string> VisibleFeatures =>
        [.. _collection.Stages.Take(CurrentStageIndex + 1).SelectMany(s => s.Features).Except(HiddenFeatures)];

    public EquatableList<string> LockedFeatures =>
    [
        .. Effects<LockEffect>().SelectMany(x => x.Effect.Features)
            .Concat(Effects<SetEffect>().Select(x => x.Effect.Feature))
            .Distinct()
    ];

    public static Configuration Start(ProductCatalog catalog, string collectionName)
    {
        var collection = catalog.Collections.FirstOrDefault(c => c.Name == collectionName);
        return collection is null
            ? throw new ArgumentException($"Collection '{collectionName}' not found in catalog.")
            : new Configuration(
                    catalog,
                    collection,
                    EquatableDictionary<string, Value>.Empty,
                    EquatableDictionary<string, Value>.Empty,
                    EquatableDictionary<string, Value>.Empty)
                .Settled();
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
        _activeRules.FirstOrDefault(r => r.Effects.Any(e => e switch
        {
            OnlyEffect only => only.Feature == featureName && !only.Options.Contains(option),
            ForbidEffect forbid => forbid.Feature == featureName && forbid.Options.Contains(option),
            OnlyFromTableEffect onlyFromTable => onlyFromTable.Feature == featureName &&
                                                 !(OptionsFromTable(onlyFromTable)?.Contains(option) ?? true),
            _ => false,
        }));

    private EquatableList<Value>? OptionsFromTable(OnlyFromTableEffect onlyFromTable)
    {
        var table = _catalog.AllowedCombinations[onlyFromTable.Table];
        return table.AllowedFor([.. table.KeyColumns.Select(ValueOf)]);
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

        return new Configuration(
                _catalog,
                _collection,
                _userValues.SetItem(featureName, value),
                _forcedValues,
                _defaultValues)
            .Settled();
    }

    private Configuration Settled()
    {
        var hidden = HiddenFeatures.ToHashSet();
        var forcedValues = Winners(ForcedValues());
        var userValues = _userValues
            .Where(kvp => !hidden.Contains(kvp.Key) && !forcedValues.ContainsKey(kvp.Key))
            .ToEquatableDictionary();

        var defaultValues = Winners(DefaultValues()
            .Where(x => !hidden.Contains(x.Feature) &&
                        !userValues.ContainsKey(x.Feature) &&
                        !forcedValues.ContainsKey(x.Feature)));

        var next = new Configuration(_catalog, _collection, userValues, forcedValues, defaultValues);
        return next._values.Equals(_values) ? next : next.Settled();
    }

    private sealed record Candidate(string Feature, Value Value, int Priority, string RuleName);

    private static EquatableDictionary<string, Value> Winners(IEnumerable<Candidate> candidates) =>
        candidates
            .GroupBy(c => c.Feature)
            .Select(feature =>
            {
                var top = feature.GroupBy(c => c.Priority).MaxBy(p => p.Key)!.ToList();
                return top.Select(c => c.Value).Distinct().Count() == 1
                    ? (feature.Key, top[0].Value)
                    : throw new InvalidOperationException(
                        $"Feature '{feature.Key}' gets different values from rules " +
                        $"{string.Join(", ", top.Select(c => $"'{c.RuleName}'"))} with the same priority.");
            })
            .ToEquatableDictionary();

    private IEnumerable<Candidate> ForcedValues() =>
        Effects<SetEffect>().Select(x => (x.Rule, x.Effect.Feature, Option: x.Effect.Value.Evaluate(_values)))
            .Where(x => x.Option is not MissingValue)
            .Select(x => new Candidate(x.Feature, x.Option, x.Rule.Priority, x.Rule.Name));

    private IEnumerable<Candidate> DefaultValues() =>
        DefaultsFromRules().Concat(DefaultsFromTables()).Concat(SingleOptionsFromTables())
            .Where(x => x.Value is not MissingValue);

    private IEnumerable<Candidate> DefaultsFromRules() =>
        Effects<DefaultEffect>()
            .Select(x =>
                new Candidate(x.Effect.Feature, x.Effect.Value.Evaluate(_values), x.Rule.Priority, x.Rule.Name));

    private IEnumerable<Candidate> DefaultsFromTables() =>
        Effects<DefaultsFromTableEffect>()
            .SelectMany(x =>
            {
                var table = _catalog.Tables[x.Effect.Table];
                EquatableList<Value> key = [.. table.KeyColumns.Select(ValueOf)];
                return x.Effect.Features.Select(f => new Candidate(f, table.Lookup(key, f), int.MinValue, x.Rule.Name));
            });

    private IEnumerable<Candidate> SingleOptionsFromTables() =>
        Effects<OnlyFromTableEffect>()
            .Select(x => (x.Rule, x.Effect.Feature, Options: OptionsFromTable(x.Effect)))
            .Where(x => x.Options is [_])
            .Select(x => new Candidate(x.Feature, x.Options![0], int.MinValue, x.Rule.Name));

    private static bool Accepts(Feature feature, Value value) =>
        (feature, value) switch
        {
            (NumberFeature, NumberValue) => true,
            (ChoiceFeature choice, OptionValue selected) => choice.Options.Any(o => o.Id == selected.Id),
            _ => false,
        };

    public ValueSource? SourceOf(string featureName) => featureName switch
    {
        _ when _forcedValues.ContainsKey(featureName) => ValueSource.Forced,
        _ when _userValues.ContainsKey(featureName) => ValueSource.User,
        _ when _defaultValues.ContainsKey(featureName) => ValueSource.Default,
        _ => null,
    };
}

public sealed record UnavailableOption(string Id, string RuleName);

public enum ValueSource
{
    Default,
    User,
    Forced,
}
