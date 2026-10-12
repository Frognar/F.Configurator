using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public sealed class Configuration
{
    private const int TablePriority = int.MinValue;

    private readonly ProductCatalog _catalog;
    private readonly Collection _collection;
    private readonly EquatableDictionary<string, Value> _userValues;
    private readonly EquatableDictionary<string, Value> _forcedValues;
    private readonly EquatableDictionary<string, Value> _defaultValues;
    private readonly EquatableDictionary<string, Value> _values;
    private readonly EquatableList<Rule> _activeRules;
    private readonly EquatableList<RuleViolation> _corrections;

    private Configuration(
        ProductCatalog catalog,
        Collection collection,
        EquatableDictionary<string, Value> userValues,
        EquatableDictionary<string, Value> forcedValues,
        EquatableDictionary<string, Value> defaultValues,
        EquatableList<RuleViolation>? corrections = null)
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

        _corrections = corrections ?? [];
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

    public EquatableList<RuleViolation> Violations =>
    [
        .. _corrections,
        .. ValueViolations().Where(v => v.Violation.Reaction != Reaction.Correct).Select(v => v.Violation)
    ];

    public bool CanFinish =>
        _collection.Features.Except(HiddenFeatures).All(_values.ContainsKey) &&
        Violations.All(e => e.Reaction != Reaction.Error);

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
                                                 OptionsFromTable(onlyFromTable) is { } allowed &&
                                                 !allowed.Contains(option),
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
        var limit = _collection.Features.Count * 2 + 2;
        var current = this;
        for (var i = 0; i < limit; i++)
        {
            var next = current.Round();
            if (next._values.Equals(current._values))
            {
                return next;
            }

            current = next;
        }

        var changing = new HashSet<string>();
        for (var i = 0; i < limit; i++)
        {
            var next = current.Round();
            changing.UnionWith(next._values.Keys.Union(current._values.Keys)
                .Where(feature => !Equals(next.ValueOf(feature), current.ValueOf(feature))));

            current = next;
        }

        throw new InvalidOperationException(
            $"Values of {string.Join(", ", changing.Order().Select(f => $"'{f}'"))} keep changing.");
    }

    private Configuration Round()
    {
        var hidden = HiddenFeatures.ToHashSet();
        var forcedValues = Winners(ForcedValues());
        var corrections = ValueViolations().Where(v => v.Violation.Reaction == Reaction.Correct).ToList();
        var userValues = corrections
            .Aggregate(_userValues, (values, c) => values.SetItem(c.Violation.Feature, c.Corrected))
            .Where(kvp => kvp.Value is not MissingValue)
            .Where(kvp => !hidden.Contains(kvp.Key) && !forcedValues.ContainsKey(kvp.Key))
            .ToEquatableDictionary();

        var defaultValues = Winners(DefaultValues()
            .Where(x => !hidden.Contains(x.Feature) &&
                        !userValues.ContainsKey(x.Feature) &&
                        !forcedValues.ContainsKey(x.Feature)));

        var next = new Configuration(_catalog, _collection, userValues, forcedValues, defaultValues,
            [.. _corrections, .. corrections.Select(c => c.Violation)]);

        return next;
    }

    private IEnumerable<(RuleViolation Violation, Value Corrected)> ValueViolations() =>
        LimitViolations().Concat(OptionViolations());

    private IEnumerable<(RuleViolation Violation, Value Corrected)> OptionViolations() =>
        _userValues
            .Where(kvp => kvp.Value is OptionValue)
            .Select(kvp => (Feature: kvp.Key, Rule: ExcludingRule(kvp.Key, kvp.Value)))
            .Where(x => x.Rule is not null)
            .Select(x => (ViolationOf(x.Feature, x.Rule), (Value)Value.Missing));

    private IEnumerable<(RuleViolation Violation, Value Corrected)> LimitViolations() =>
        _userValues
            .Select(kvp => (Number: _catalog.Features.OfType<NumberFeature>().FirstOrDefault(f => f.Name == kvp.Key),
                kvp.Value))
            .Where(x => x is { Number: not null, Value: NumberValue })
            .SelectMany(x => LimitViolations(x.Number!, ((NumberValue)x.Value).Amount));

    private IEnumerable<(RuleViolation Violation, Value Corrected)> LimitViolations(NumberFeature number,
        decimal amount)
    {
        var (min, minRule) = MinOf(number);
        var (max, maxRule) = MaxOf(number);
        var (step, stepRule) = StepOf(number);
        var corrected = Value.Number(Clamp(RoundToStep(amount, step), min, max));

        if (step is { } s && amount % s != 0)
        {
            yield return (ViolationOf(number.Name, stepRule), corrected);
        }

        if (amount < min)
        {
            yield return (ViolationOf(number.Name, minRule), corrected);
        }

        if (amount > max)
        {
            yield return (ViolationOf(number.Name, maxRule), corrected);
        }
    }

    private static decimal RoundToStep(decimal amount, decimal? step) =>
        step is { } s ? Math.Round(amount / s, MidpointRounding.AwayFromZero) * s : amount;

    private static decimal Clamp(decimal amount, decimal? min, decimal? max) =>
        Math.Min(Math.Max(amount, min ?? amount), max ?? decimal.MaxValue);

    private RuleViolation ViolationOf(string featureName, Rule? rule) =>
        new(featureName,
            rule?.Name,
            rule?.Violation?.Reaction ?? _catalog.DefaultReaction ?? Reaction.Error,
            rule?.Violation?.Message);

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
        Effects<SetEffect>().Select(x => (x.Rule, x.Effect.Feature, Value: x.Effect.Value.Evaluate(_values)))
            .Select(x => new Candidate(x.Feature, x.Value, x.Rule.Priority, x.Rule.Name));

    private IEnumerable<Candidate> DefaultValues() =>
        DefaultsFromRules().Concat(DefaultsFromTables()).Concat(SingleOptionsFromTables());

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
                return x.Effect.Features.Select(f =>
                    new Candidate(f, table.Lookup(key, f), TablePriority, x.Rule.Name));
            });

    private IEnumerable<Candidate> SingleOptionsFromTables() =>
        Effects<OnlyFromTableEffect>()
            .Select(x => (x.Rule, x.Effect.Feature, Options: OptionsFromTable(x.Effect)))
            .Where(x => x.Options is [_])
            .Select(x => new Candidate(x.Feature, x.Options![0], TablePriority, x.Rule.Name));

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

    public NumberLimits LimitsOf(string featureName)
    {
        var number = _catalog.Features.OfType<NumberFeature>().FirstOrDefault(f => f.Name == featureName);
        if (number is null)
        {
            throw new ArgumentException($"Feature '{featureName}' is not a number feature.");
        }

        return new NumberLimits(MinOf(number).Amount, MaxOf(number).Amount, StepOf(number).Amount);
    }

    private (decimal? Amount, Rule? Rule) MinOf(NumberFeature number) =>
        Limit<MinEffect>(number.Name, e => (e.Feature, e.Value), Enumerable.Max, number.Min);

    private (decimal? Amount, Rule? Rule) MaxOf(NumberFeature number) =>
        Limit<MaxEffect>(number.Name, e => (e.Feature, e.Value), Enumerable.Min, number.Max);

    private (decimal? Amount, Rule? Rule) StepOf(NumberFeature number) =>
        Limit<StepEffect>(number.Name, e => (e.Feature, e.Value),
            steps => steps.Aggregate(DecimalMath.LeastCommonMultiple), number.Step, step => step > 0);

    private (decimal? Amount, Rule? Rule) Limit<T>(
        string featureName,
        Func<T, (string Feature, Expression Value)> parts,
        Func<IEnumerable<decimal>, decimal> mostRestrictive,
        decimal? declared,
        Func<decimal, bool>? accepts = null) where T : Effect
    {
        var top = Effects<T>()
            .Select(x => (x.Rule, Parts: parts(x.Effect)))
            .Where(x => x.Parts.Feature == featureName)
            .Select(x => (x.Rule, Value: x.Parts.Value.Evaluate(_values)))
            .Where(x => x.Value is NumberValue number && (accepts?.Invoke(number.Amount) ?? true))
            .Select(x => (x.Rule, ((NumberValue)x.Value).Amount))
            .GroupBy(x => x.Rule.Priority)
            .MaxBy(g => g.Key);

        if (top is null)
        {
            return (declared, null);
        }

        var amount = mostRestrictive(top.Select(x => x.Amount));
        return (amount, top.FirstOrDefault(x => x.Amount == amount).Rule ?? top.First().Rule);
    }
}

public sealed record UnavailableOption(string Id, string RuleName);

public sealed record NumberLimits(decimal? Min, decimal? Max, decimal? Step);

public sealed record RuleViolation(string Feature, string? RuleName, Reaction Reaction, string? Message);

public enum ValueSource
{
    Default,
    User,
    Forced,
}
