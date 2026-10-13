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
    private readonly EquatableList<RuleViolation> _corrections;
    private readonly EquatableDictionary<string, Value> _values;
    private readonly EquatableList<Rule> _activeRules;
    private readonly HashSet<string> _hiddenFeatures;

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
        _corrections = corrections ?? [];

        _values = userValues.Concat(forcedValues)
            .Aggregate(defaultValues, (values, kvp) => values.SetItem(kvp.Key, kvp.Value));

        var computedValues = _catalog.Features.OfType<ComputedFeature>()
            .Select(f => (f.Name, Value: f.Expression.Evaluate(_values)))
            .Where(x => x.Value is not MissingValue)
            .ToEquatableDictionary();

        _activeRules =
        [
            .. _catalog.Rules.Where(r =>
                (r.Collections.Count == 0 || r.Collections.Contains(_collection.Name)) &&
                (r.Condition is null || r.Condition.Evaluate(_values) is BooleanValue { IsTrue: true } ||
                 r.Condition.Evaluate(computedValues) is BooleanValue { IsTrue: true }))
        ];

        _hiddenFeatures =
        [
            .. Effects<HideEffect>().SelectMany(x => x.Effect.Features),
            .. Effects<OnlyFromTableEffect>()
                .Where(x => OptionsFromTable(x.Effect) is { Count: 0 })
                .Select(x => x.Effect.Feature)
        ];
    }

    public static Configuration Start(ProductCatalog catalog, string collectionName)
    {
        var collection = catalog.Collections.FirstOrDefault(c => c.Name == collectionName)
                         ?? throw new ArgumentException($"Collection '{collectionName}' not found in catalog.");

        return new Configuration(
                catalog,
                collection,
                EquatableDictionary<string, Value>.Empty,
                EquatableDictionary<string, Value>.Empty,
                EquatableDictionary<string, Value>.Empty)
            .Settled();
    }

    public Configuration Set(string featureName, Value value)
    {
        EnsureCanSet(featureName, value);

        return new Configuration(
                _catalog,
                _collection,
                _userValues.SetItem(featureName, value),
                _forcedValues,
                _defaultValues)
            .Settled();
    }

    public string CurrentStage => _collection.Stages[CurrentStageIndex].Name;

    public EquatableList<string> VisibleFeatures =>
        [.. _collection.Stages.Take(CurrentStageIndex + 1).SelectMany(s => s.Features).Except(_hiddenFeatures)];

    public EquatableList<string> LockedFeatures =>
    [
        .. Effects<LockEffect>().SelectMany(x => x.Effect.Features)
            .Concat(Effects<SetEffect>().Select(x => x.Effect.Feature))
            .Distinct()
    ];

    public Value ValueOf(string featureName) =>
        _catalog.Features.OfType<ComputedFeature>().FirstOrDefault(f => f.Name == featureName) is { } computed
            ? computed.Expression.Evaluate(_values)
            : _values.GetValueOrDefault(featureName, Value.Missing);

    public ValueSource? SourceOf(string featureName) => featureName switch
    {
        _ when _forcedValues.ContainsKey(featureName) => ValueSource.Forced,
        _ when _userValues.ContainsKey(featureName) => ValueSource.User,
        _ when _defaultValues.ContainsKey(featureName) => ValueSource.Default,
        _ when _catalog.Features.OfType<ComputedFeature>().Any(f => f.Name == featureName) &&
               ValueOf(featureName) is not MissingValue => ValueSource.Computed,
        _ => null,
    };

    public EquatableList<string> AvailableOptions(string featureName) =>
    [
        .. ChoiceFeatureOf(featureName).Options.Select(o => o.Id)
            .Except(UnavailableOptions(featureName).Select(u => u.Id))
    ];

    public EquatableList<UnavailableOption> UnavailableOptions(string featureName) =>
    [
        .. ChoiceFeatureOf(featureName).Options
            .Select(o => (o.Id, Rule: ExcludingRule(featureName, Value.Option(o.Id))))
            .Where(x => x.Rule is not null)
            .Select(x => new UnavailableOption(x.Id, x.Rule!.Name))
    ];

    public NumberLimits LimitsOf(string featureName)
    {
        var number = NumberFeatureOf(featureName);
        return new NumberLimits(MinOf(number).Amount, MaxOf(number).Amount, StepOf(number).Amount);
    }

    public TextLength LengthOf(string featureName)
    {
        var text = TextFeatureOf(featureName);
        return new TextLength(MinLengthOf(text).Amount, MaxLengthOf(text).Amount);
    }

    public EquatableList<RuleViolation> Violations =>
    [
        .. _corrections,
        .. ValueViolations().Where(v => v.Violation.Reaction != Reaction.Correct).Select(v => v.Violation)
    ];

    public bool CanFinish => AllSet(_collection.Features) && Violations.All(v => v.Reaction != Reaction.Error);

    private int CurrentStageIndex
    {
        get
        {
            var i = _collection.Stages.ToList().FindIndex(s => !AllSet(s.Features));
            return i < 0 ? _collection.Stages.Count - 1 : i;
        }
    }

    private bool AllSet(IEnumerable<string> features) => features.Except(_hiddenFeatures).All(_values.ContainsKey);

    private void EnsureCanSet(string featureName, Value value)
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
    }

    private static bool Accepts(Feature feature, Value value) =>
        (feature, value) switch
        {
            (NumberFeature, NumberValue) => true,
            (TextFeature, TextValue) => true,
            (ChoiceFeature choice, OptionValue selected) => choice.Options.Any(o => o.Id == selected.Id),
            _ => false,
        };

    private ChoiceFeature ChoiceFeatureOf(string featureName) =>
        _catalog.Features.OfType<ChoiceFeature>().FirstOrDefault(f => f.Name == featureName)
        ?? throw new ArgumentException($"Feature '{featureName}' is not a choice feature in catalog.");

    private NumberFeature NumberFeatureOf(string featureName) =>
        _catalog.Features.OfType<NumberFeature>().FirstOrDefault(f => f.Name == featureName)
        ?? throw new ArgumentException($"Feature '{featureName}' is not a number feature.");

    private TextFeature TextFeatureOf(string featureName) =>
        _catalog.Features.OfType<TextFeature>().FirstOrDefault(f => f.Name == featureName)
        ?? throw new ArgumentException($"Feature '{featureName}' is not a text feature.");

    private IEnumerable<(Rule Rule, T Effect)> Effects<T>() where T : Effect =>
        _activeRules.SelectMany(r => r.Effects.OfType<T>().Select(e => (r, e)));

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
        return table.AllowedFor(KeyOf(table.KeyColumns));
    }

    private EquatableList<Value> KeyOf(IEnumerable<string> keyColumns) => [.. keyColumns.Select(ValueOf)];

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
            changing.UnionWith(current.ChangedFeatures(next));
            current = next;
        }

        throw new InvalidOperationException(
            $"Values of {string.Join(", ", changing.Order().Select(f => $"'{f}'"))} keep changing.");
    }

    private IEnumerable<string> ChangedFeatures(Configuration next) =>
        _values.Keys.Union(next._values.Keys).Where(f => !Equals(ValueOf(f), next.ValueOf(f)));

    private Configuration Round()
    {
        var forcedValues = Winners(ForcedValues());
        var corrections = ValueViolations().Where(v => v.Violation.Reaction == Reaction.Correct).ToList();
        var userValues = corrections
            .Aggregate(_userValues, (values, c) => values.SetItem(c.Violation.Feature, c.Corrected))
            .Where(kvp => kvp.Value is not MissingValue &&
                          !_hiddenFeatures.Contains(kvp.Key) &&
                          !forcedValues.ContainsKey(kvp.Key))
            .ToEquatableDictionary();

        var defaultValues = Winners(DefaultValues()
            .Where(x => !_hiddenFeatures.Contains(x.Feature) &&
                        !userValues.ContainsKey(x.Feature) &&
                        !forcedValues.ContainsKey(x.Feature)));

        return new Configuration(_catalog, _collection, userValues, forcedValues, defaultValues,
            [.. _corrections, .. corrections.Select(c => c.Violation)]);
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
        Effects<SetEffect>()
            .Select(x =>
                new Candidate(x.Effect.Feature, x.Effect.Value.Evaluate(_values), x.Rule.Priority, x.Rule.Name));

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
                var key = KeyOf(table.KeyColumns);
                return x.Effect.Features
                    .Select(f => new Candidate(f, table.Lookup(key, f), TablePriority, x.Rule.Name));
            });

    private IEnumerable<Candidate> SingleOptionsFromTables() =>
        Effects<OnlyFromTableEffect>()
            .Select(x => (x.Rule, x.Effect.Feature, Options: OptionsFromTable(x.Effect)))
            .Where(x => x.Options is [_])
            .Select(x => new Candidate(x.Feature, x.Options![0], TablePriority, x.Rule.Name));

    private (decimal? Amount, Rule? Rule) MinOf(NumberFeature number) =>
        Limit<MinEffect>(number.Name, Enumerable.Max, number.Min);

    private (decimal? Amount, Rule? Rule) MaxOf(NumberFeature number) =>
        Limit<MaxEffect>(number.Name, Enumerable.Min, number.Max);

    private (decimal? Amount, Rule? Rule) StepOf(NumberFeature number) =>
        Limit<StepEffect>(number.Name, steps => steps.Aggregate(DecimalMath.LeastCommonMultiple), number.Step,
            step => step > 0);

    private (decimal? Amount, Rule? Rule) Limit<T>(
        string featureName,
        Func<IEnumerable<decimal>, decimal> mostRestrictive,
        decimal? declared,
        Func<decimal, bool>? accepts = null) where T : LimitEffect
    {
        accepts ??= _ => true;
        var top = Effects<T>()
            .Where(x => x.Effect.Feature == featureName)
            .Select(x => (x.Rule, Value: x.Effect.Value.Evaluate(_values)))
            .Where(x => x.Value is NumberValue number && accepts(number.Amount))
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

    private (int? Amount, Rule? Rule) MinLengthOf(TextFeature text) =>
        LengthLimit(text.Name, range => range.Min, Enumerable.Max, text.MinLength);

    private (int? Amount, Rule? Rule) MaxLengthOf(TextFeature text) =>
        LengthLimit(text.Name, range => range.Max, Enumerable.Min, text.MaxLength);

    private (int? Amount, Rule? Rule) LengthLimit(
        string featureName,
        Func<RangeValue, decimal> bound,
        Func<IEnumerable<decimal>, decimal> mostRestrictive,
        int? declared)
    {
        var top = Effects<LengthEffect>()
            .Where(x => x.Effect.Feature == featureName)
            .Select(x => (x.Rule, Value: x.Effect.Value.Evaluate(_values)))
            .Where(x => x.Value is RangeValue)
            .Select(x => (x.Rule, Amount: bound((RangeValue)x.Value)))
            .GroupBy(x => x.Rule.Priority)
            .MaxBy(g => g.Key);

        if (top is null)
        {
            return (declared, null);
        }

        var amount = mostRestrictive(top.Select(x => x.Amount));
        return (decimal.ToInt32(amount), top.First(x => x.Amount == amount).Rule);
    }

    private IEnumerable<(RuleViolation Violation, Value Corrected)> ValueViolations() =>
        LimitViolations().Concat(LengthViolations()).Concat(OptionViolations());

    private IEnumerable<(RuleViolation Violation, Value Corrected)> LimitViolations() =>
        _catalog.Features.OfType<NumberFeature>()
            .Select(number => (Number: number, Value: _userValues.GetValueOrDefault(number.Name)))
            .Where(x => x.Value is NumberValue)
            .SelectMany(x => LimitViolations(x.Number, ((NumberValue)x.Value!).Amount));

    private IEnumerable<(RuleViolation Violation, Value Corrected)> LimitViolations(
        NumberFeature number,
        decimal amount)
    {
        var (min, minRule) = MinOf(number);
        var (max, maxRule) = MaxOf(number);
        var (step, stepRule) = StepOf(number);
        var corrected = Value.Number(OnStepWithin(RoundToStep(amount, step), min, max, step));

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

    private IEnumerable<(RuleViolation Violation, Value Corrected)> LengthViolations() =>
        _catalog.Features.OfType<TextFeature>()
            .Select(text => (Text: text, Value: _userValues.GetValueOrDefault(text.Name)))
            .Where(x => x.Value is TextValue)
            .SelectMany(x => LengthViolations(x.Text, ((TextValue)x.Value!).Content));

    // A too short text has no nearest allowed text, so the correction clears it; a too long one keeps its start.
    private IEnumerable<(RuleViolation Violation, Value Corrected)> LengthViolations(TextFeature text, string content)
    {
        var (min, minRule) = MinLengthOf(text);
        var (max, maxRule) = MaxLengthOf(text);

        if (content.Length < min)
        {
            yield return (ViolationOf(text.Name, minRule), Value.Missing);
        }

        if (content.Length > max)
        {
            yield return (ViolationOf(text.Name, maxRule), Value.Text(content[..max.Value]));
        }
    }

    private IEnumerable<(RuleViolation Violation, Value Corrected)> OptionViolations() =>
        _userValues
            .Where(kvp => kvp.Value is OptionValue)
            .Select(kvp => (Feature: kvp.Key, Rule: ExcludingRule(kvp.Key, kvp.Value)))
            .Where(x => x.Rule is not null)
            .Select(x => (ViolationOf(x.Feature, x.Rule), (Value)Value.Missing));

    private static decimal RoundToStep(decimal amount, decimal? step) =>
        step is { } s ? Math.Round(amount / s, MidpointRounding.AwayFromZero) * s : amount;

    // Outside the limits the value goes to the nearest multiple of the step that lies inside them.
    private static decimal OnStepWithin(decimal amount, decimal? min, decimal? max, decimal? step) => amount switch
    {
        _ when amount > max => step is { } s ? Math.Floor(max.Value / s) * s : max.Value,
        _ when amount < min => step is { } s ? Math.Ceiling(min.Value / s) * s : min.Value,
        _ => amount,
    };

    private RuleViolation ViolationOf(string featureName, Rule? rule) =>
        new(featureName,
            rule?.Name,
            rule?.Violation?.Reaction ?? _catalog.DefaultReaction ?? Reaction.Error,
            rule?.Violation?.Message);
}
