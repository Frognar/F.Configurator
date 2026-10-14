using F.Configurator.Catalog;
using F.Configurator.Expressions;
using F.Configurator.Results;

namespace F.Configurator.Engine;

public sealed class Configuration
{
    private const int TablePriority = int.MinValue;

    private readonly ProductCatalog _catalog;
    private readonly Collection _collection;
    private readonly EquatableDictionary<string, Value> _userValues;
    private readonly EquatableDictionary<string, Value> _forcedValues;
    private readonly EquatableDictionary<string, Value> _defaultValues;
    private readonly EquatableDictionary<string, Value> _defaultsOf;
    private readonly EquatableDictionary<string, Value> _context;
    private readonly EquatableList<RuleViolation> _corrections;
    private readonly EquatableList<ValueChange> _changes;
    private readonly EquatableDictionary<string, Value> _values;
    private readonly EquatableList<Rule> _activeRules;
    private readonly HashSet<string> _hiddenFeatures;

    private Configuration(
        ProductCatalog catalog,
        Collection collection,
        EquatableDictionary<string, Value> userValues,
        EquatableDictionary<string, Value> forcedValues,
        EquatableDictionary<string, Value> defaultValues,
        EquatableDictionary<string, Value> defaultsOf,
        EquatableList<RuleViolation>? corrections = null,
        EquatableList<ValueChange>? changes = null,
        EquatableDictionary<string, Value>? context = null)
    {
        _catalog = catalog;
        _collection = collection;
        _userValues = userValues;
        _forcedValues = forcedValues;
        _defaultValues = defaultValues;
        _defaultsOf = defaultsOf;
        _corrections = corrections ?? [];
        _changes = changes ?? [];
        _context = context ?? EquatableDictionary<string, Value>.Empty;

        var values = userValues.Concat(forcedValues)
            .Concat(defaultsOf.Select(kvp => KeyValuePair.Create(DefaultOf.KeyOf(kvp.Key), kvp.Value)))
            .Aggregate(defaultValues, (all, kvp) => all.SetItem(kvp.Key, kvp.Value));

        // Each computed feature sees the ones declared before it.
        _values = _catalog.Features.OfType<ComputedFeature>()
            .Aggregate(values, (all, feature) =>
                feature.Expression.Evaluate(all, _context) is var value and not MissingValue
                    ? all.SetItem(feature.Name, value)
                    : all);

        _activeRules =
        [
            .. _catalog.Rules.Where(r =>
                (r.Collections.Count == 0 || r.Collections.Contains(_collection.Name)) &&
                (r.Condition is null || r.Condition.Evaluate(_values, _context) is BooleanValue { IsTrue: true }))
        ];

        _hiddenFeatures =
        [
            .. Effects<HideEffect>().SelectMany(x => x.Effect.Features),
            .. Effects<OnlyFromTableEffect>()
                .Where(x => OptionsFromTable(x.Effect) is { Count: 0 })
                .Select(x => x.Effect.Feature)
        ];
    }

    public static Configuration Start(ProductCatalog catalog, string collectionName) =>
        Start(catalog, collectionName, EquatableDictionary<string, Value>.Empty);

    public static Configuration Start(
        ProductCatalog catalog,
        string collectionName,
        IReadOnlyDictionary<string, Value> context)
    {
        var collection = catalog.Collections.FirstOrDefault(c => c.Name == collectionName)
                         ?? throw new ArgumentException($"Collection '{collectionName}' not found in catalog.");
        EnsureDeclared(catalog, context);

        return new Configuration(
                catalog,
                collection,
                EquatableDictionary<string, Value>.Empty,
                EquatableDictionary<string, Value>.Empty,
                EquatableDictionary<string, Value>.Empty,
                EquatableDictionary<string, Value>.Empty,
                context: context.ToEquatableDictionary())
            .Settled();
    }

    public Configuration Set(string featureName, Value value)
    {
        EnsureCanSet(featureName, value);

        var next = new Configuration(
                _catalog,
                _collection,
                _userValues.SetItem(featureName, value),
                _forcedValues,
                _defaultValues,
                _defaultsOf,
                context: _context)
            .Settled();

        return next.WithChanges(ChangesTo(next, featureName, value));
    }

    public string CurrentStage => _collection.Stages[CurrentStageIndex].Name;

    public EquatableList<string> VisibleFeatures =>
        [.. _collection.Stages.Take(CurrentStageIndex + 1).SelectMany(s => s.Features).Except(_hiddenFeatures)];

    public EquatableList<string> LockedFeatures =>
    [
        .. Effects<LockEffect>().SelectMany(x => x.Effect.Features)
            .Concat(Effects<SetEffect>().Select(x => x.Effect.Feature))
            .Concat(_collection.Features.Where(IsComputed))
            .Distinct()
    ];

    public Value ValueOf(string featureName) => _values.GetValueOrDefault(featureName, Value.Missing);

    public ValueSource? SourceOf(string featureName) => featureName switch
    {
        _ when _forcedValues.ContainsKey(featureName) => ValueSource.Forced,
        _ when _userValues.ContainsKey(featureName) => ValueSource.User,
        _ when _defaultValues.ContainsKey(featureName) => ValueSource.Default,
        _ when _values.ContainsKey(featureName) && IsComputed(featureName) => ValueSource.Computed,
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
        .. ValueViolations().Where(v => v.Violation.Reaction != Reaction.Correct).Select(v => v.Violation),
        .. ForcedViolations()
    ];

    public EquatableList<RuleMessage> Messages =>
        [.. Effects<MessageEffect>().Select(x => new RuleMessage(x.Rule.Name, x.Effect.Severity, x.Effect.Text))];

    public EquatableList<ValueChange> Changes => _changes;

    public string? Index =>
        AllSet(_collection.Features) && _collection.Index?.Evaluate(_values) is TextValue index ? index.Content : null;

    public StepResult Result =>
        new(_catalog.Name,
            _catalog.Version,
            _collection.Name,
            _context,
            [.. ResultFeatures.Select(FeatureResultOf)],
            [
                .. Violations.Select(v => new StepMessage(KindOf(v.Reaction), v.Message, v.RuleName, v.Feature))
                    .Concat(Messages.Select(m => new StepMessage(KindOf(m.Severity), m.Text, m.RuleName, null)))
                    .OrderByDescending(m => m.Kind)
                    .ThenBy(m => m.RuleName, StringComparer.Ordinal)
            ],
            Changes,
            Index,
            AllRequiredSet(_collection.Features));

    public bool CanFinish =>
        AllRequiredSet(_collection.Features) && Violations.All(v => v.Reaction != Reaction.Error) &&
        Messages.All(m => m.Severity != Severity.Error);

    private int CurrentStageIndex
    {
        get
        {
            var i = _collection.Stages.ToList().FindIndex(s => !AllRequiredSet(s.Features));
            return i < 0 ? _collection.Stages.Count - 1 : i;
        }
    }

    private bool AllSet(IEnumerable<string> features) => features.Except(_hiddenFeatures).All(_values.ContainsKey);

    private bool AllRequiredSet(IEnumerable<string> features) => AllSet(features.Where(IsRequired));

    private bool IsRequired(string featureName) => _catalog.Features.First(f => f.Name == featureName).IsRequired;

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

    // Grammar 4.6: `Kontrahent` and `Język` are built in; other context values are declared attributes.
    private static readonly string[] BuiltInContext = ["Kontrahent", "Język"];

    private static void EnsureDeclared(ProductCatalog catalog, IReadOnlyDictionary<string, Value> context)
    {
        var attributes = catalog.Contexts.Values
            .SelectMany(c => c.Attributes.Select(a => (Name: $"{c.Name}.{a.Name}", a.Type)))
            .ToDictionary(a => a.Name, a => a.Type);

        foreach (var (name, value) in context)
        {
            if (attributes.TryGetValue(name, out var type) && !IsOfType(value, type))
            {
                throw new ArgumentException($"Context value '{name}' is not of type {type}.");
            }

            if (!attributes.ContainsKey(name) && !BuiltInContext.Contains(name))
            {
                throw new ArgumentException($"Context value '{name}' is not declared in catalog.");
            }
        }
    }

    private static bool IsOfType(Value value, AttributeType type) =>
        (value, type) switch
        {
            (TextValue, AttributeType.Text) => true,
            (BooleanValue, AttributeType.Boolean) => true,
            (NumberValue, AttributeType.Number) => true,
            _ => false,
        };

    private static bool Accepts(Feature feature, Value value) =>
        (feature, value) switch
        {
            (NumberFeature, NumberValue) => true,
            (TextFeature, TextValue) => true,
            (ChoiceFeature choice, OptionValue selected) => choice.Options.Any(o => o.Id == selected.Id),
            _ => false,
        };

    private bool IsComputed(string featureName) =>
        _catalog.Features.OfType<ComputedFeature>().Any(f => f.Name == featureName);

    // Computed features are not always in the stages, so they come after the features of the collection.
    private IEnumerable<string> ResultFeatures =>
        _collection.Features
            .Concat(_catalog.Features.OfType<ComputedFeature>().Select(computed => computed.Name))
            .Distinct();

    private FeatureResult FeatureResultOf(string featureName)
    {
        var chosen = ValueOf(featureName) is OptionValue option
            ? ChoiceFeatureOf(featureName).Options.First(o => o.Id == option.Id)
            : null;

        return new FeatureResult(
            featureName,
            ValueOf(featureName),
            chosen?.Symbol,
            SourceOf(featureName),
            VisibleFeatures.Contains(featureName),
            chosen?.Attributes ?? EquatableDictionary<string, Value>.Empty);
    }

    private static MessageKind KindOf(Reaction reaction) => reaction switch
    {
        Reaction.Correct => MessageKind.Information,
        Reaction.Warning => MessageKind.Warning,
        _ => MessageKind.Error,
    };

    private static MessageKind KindOf(Severity severity) => severity switch
    {
        Severity.Information => MessageKind.Information,
        Severity.Warning => MessageKind.Warning,
        _ => MessageKind.Error,
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
        _activeRules
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(ReactionOf)
            .ThenBy(r => r.Name, StringComparer.Ordinal)
            .FirstOrDefault(r => r.Effects.Any(e => e switch
            {
                OnlyEffect only => only.Feature == featureName && !only.Options.Contains(option),
                ForbidEffect forbid => forbid.Feature == featureName && forbid.Options.Contains(option),
                OnlyFromTableEffect onlyFromTable => onlyFromTable.Feature == featureName &&
                                                     OptionsFromTable(onlyFromTable) is { } allowed &&
                                                     !allowed.Contains(option),
                _ => false,
            }));

    private Rule? HidingRule(string featureName) =>
        Effects<HideEffect>().Where(x => x.Effect.Features.Contains(featureName)).Select(x => x.Rule)
            .Concat(Effects<OnlyFromTableEffect>()
                .Where(x => x.Effect.Feature == featureName && OptionsFromTable(x.Effect) is { Count: 0 })
                .Select(x => x.Rule))
            .OrderBy(r => r.Name, StringComparer.Ordinal)
            .FirstOrDefault();

    private EquatableList<Value>? OptionsFromTable(OnlyFromTableEffect onlyFromTable)
    {
        var table = _catalog.AllowedCombinations[onlyFromTable.Table];
        return table.AllowedFor(KeyOf(table.KeyColumns));
    }

    private EquatableList<Value> KeyOf(IEnumerable<string> keyColumns) => [.. keyColumns.Select(ValueOf)];

    private Configuration WithChanges(EquatableList<ValueChange> changes) =>
        new(_catalog, _collection, _userValues, _forcedValues, _defaultValues, _defaultsOf, _corrections, changes,
            _context);

    // The changed feature counts from the entered value, so a rule that changes it is listed.
    private EquatableList<ValueChange> ChangesTo(Configuration next, string featureName, Value entered) =>
    [
        .. ResultFeatures
            .Select(f => (Feature: f, Before: f == featureName ? entered : ValueOf(f), After: next.ValueOf(f)))
            .Where(x => !Equals(x.Before, x.After))
            .Select(x => next.ChangeOf(x.Feature, x.Before, x.After))
    ];

    private ValueChange ChangeOf(string featureName, Value before, Value after)
    {
        var (cause, rule) = featureName switch
        {
            _ when _corrections.FirstOrDefault(c => c.Feature == featureName) is { } correction =>
                (ChangeCause.Corrected, correction.RuleName),
            _ when _hiddenFeatures.Contains(featureName) => (ChangeCause.Hidden, HidingRule(featureName)?.Name),
            _ when IsComputed(featureName) => (ChangeCause.Computed, null),
            _ when _forcedValues.ContainsKey(featureName) =>
                (ChangeCause.Forced, WinningRule(ForcedValues(), featureName)),
            _ when _defaultValues.ContainsKey(featureName) =>
                (ChangeCause.Default, WinningRule(DefaultValues(), featureName)),
            _ => (ChangeCause.Removed, null),
        };

        return new ValueChange(featureName, before, after, cause, rule);
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

        var defaults = DefaultValues()
            .Where(x => !_hiddenFeatures.Contains(x.Feature))
            .Where(x => WithinDomain(x.Feature, x.Value))
            .ToList();
        var defaultValues = Winners(defaults
            .Where(x => !userValues.ContainsKey(x.Feature) && !forcedValues.ContainsKey(x.Feature)));

        return new Configuration(_catalog, _collection, userValues, forcedValues, defaultValues, Unanimous(defaults),
            [.. _corrections, .. corrections.Select(c => c.Violation)], context: _context);
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

    private static EquatableDictionary<string, Value> Unanimous(IEnumerable<Candidate> candidates) =>
        candidates
            .GroupBy(c => c.Feature)
            .Select(feature => (feature.Key,
                Values: feature.GroupBy(c => c.Priority).MaxBy(p => p.Key)!.Select(c => c.Value).Distinct().ToList()))
            .Where(x => x.Values is [_])
            .Select(x => (x.Key, x.Values[0]))
            .ToEquatableDictionary();

    private static string? WinningRule(IEnumerable<Candidate> candidates, string featureName) =>
        candidates.Where(c => c.Feature == featureName).GroupBy(c => c.Priority).MaxBy(p => p.Key)?
            .Select(c => c.RuleName).Order(StringComparer.Ordinal).First();

    private IEnumerable<Candidate> ForcedValues() =>
        Effects<SetEffect>()
            .Select(x => new Candidate(
                x.Effect.Feature, x.Effect.Value.Evaluate(_values, _context), x.Rule.Priority, x.Rule.Name));

    private IEnumerable<Candidate> DefaultValues() =>
        DefaultsFromRules().Concat(DefaultsFromTables()).Concat(SingleOptionsFromTables());

    private IEnumerable<Candidate> DefaultsFromRules() =>
        Effects<DefaultEffect>()
            .Select(x => new Candidate(
                x.Effect.Feature, x.Effect.Value.Evaluate(_values, _context), x.Rule.Priority, x.Rule.Name));

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
            .Select(x => (x.Rule, Value: x.Effect.Value.Evaluate(_values, _context)))
            .Where(x => x.Value is NumberValue number && accepts(number.Amount))
            .Select(x => (x.Rule, ((NumberValue)x.Value).Amount))
            .GroupBy(x => x.Rule.Priority)
            .MaxBy(g => g.Key);

        if (top is null)
        {
            return (declared, null);
        }

        var amount = mostRestrictive(top.Select(x => x.Amount));
        var rule = top.Where(x => x.Amount == amount)
            .OrderByDescending(x => ReactionOf(x.Rule))
            .ThenBy(x => x.Rule.Name, StringComparer.Ordinal)
            .Select(x => x.Rule)
            .FirstOrDefault();
        var combined = top
            .OrderByDescending(x => ReactionOf(x.Rule))
            .ThenBy(x => x.Rule.Name, StringComparer.Ordinal)
            .First().Rule;
        return (amount, rule ?? combined);
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
            .Select(x => (x.Rule, Value: x.Effect.Value.Evaluate(_values, _context)))
            .Where(x => x.Value is RangeValue)
            .Select(x => (x.Rule, Amount: bound((RangeValue)x.Value)))
            .GroupBy(x => x.Rule.Priority)
            .MaxBy(g => g.Key);

        if (top is null)
        {
            return (declared, null);
        }

        var amount = mostRestrictive(top.Select(x => x.Amount));
        var rule = top.Where(x => x.Amount == amount)
            .OrderByDescending(x => ReactionOf(x.Rule))
            .ThenBy(x => x.Rule.Name, StringComparer.Ordinal)
            .First().Rule;
        return (decimal.ToInt32(amount), rule);
    }

    private bool WithinDomain(string featureName, Value value) => !DomainViolations(featureName, value).Any();

    private IEnumerable<(RuleViolation Violation, Value Corrected)> ValueViolations() =>
        DomainViolations(_userValues);

    private IEnumerable<(RuleViolation Violation, Value Corrected)> DomainViolations(
        IReadOnlyDictionary<string, Value> values) =>
        _catalog.Features
            .Where(feature => values.ContainsKey(feature.Name))
            .SelectMany(feature => DomainViolations(feature.Name, values[feature.Name]));

    private IEnumerable<(RuleViolation Violation, Value Corrected)> DomainViolations(string featureName, Value value) =>
        value switch
        {
            OptionValue => ExcludingRule(featureName, value) is { } rule
                ? [(ViolationOf(featureName, rule), Value.Missing)]
                : [],
            NumberValue number => LimitViolations(NumberFeatureOf(featureName), number.Amount),
            TextValue text => LengthViolations(TextFeatureOf(featureName), text.Content),
            _ => [],
        };

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

    // A forced value cannot be skipped like a default, so outside its domain the catalog contradicts itself.
    private IEnumerable<RuleViolation> ForcedViolations() =>
        DomainViolations(_forcedValues).Select(v => v.Violation with { Reaction = Reaction.Error });

    private static decimal RoundToStep(decimal amount, decimal? step) =>
        step is { } s ? Math.Round(amount / s, MidpointRounding.AwayFromZero) * s : amount;

    // Outside the limits the value goes to the nearest multiple of the step that lies inside them.
    private static decimal OnStepWithin(decimal amount, decimal? min, decimal? max, decimal? step) => amount switch
    {
        _ when amount > max => step is { } s ? Math.Floor(max.Value / s) * s : max.Value,
        _ when amount < min => step is { } s ? Math.Ceiling(min.Value / s) * s : min.Value,
        _ => amount,
    };

    private Reaction ReactionOf(Rule rule) => rule.Violation?.Reaction ?? _catalog.DefaultReaction ?? Reaction.Error;

    private RuleViolation ViolationOf(string featureName, Rule? rule) =>
        new(featureName,
            rule?.Name,
            rule?.Violation?.Reaction ?? _catalog.DefaultReaction ?? Reaction.Error,
            rule?.Violation?.Message);
}
