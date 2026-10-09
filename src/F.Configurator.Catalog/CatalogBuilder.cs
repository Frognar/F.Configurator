using System.Collections.Immutable;
using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public sealed class CatalogBuilder
{
    private readonly Catalog _catalog;

    private CatalogBuilder(Catalog catalog) => _catalog = catalog;

    public static CatalogBuilder Create(string name) =>
        new(new Catalog(name,
            ImmutableList<Feature>.Empty,
            ImmutableList<Collection>.Empty,
            ImmutableList<Rule>.Empty,
            ImmutableDictionary<string, Table>.Empty,
            ImmutableDictionary<string, AllowedCombinationsTable>.Empty));

    private CatalogBuilder AddFeature(Feature feature) =>
        _catalog.Features.Any(f => f.Name == feature.Name)
            ? throw Duplicate("Feature", feature.Name)
            : new CatalogBuilder(_catalog with { Features = _catalog.Features.Add(feature) });

    public CatalogBuilder Choice(string name, Func<ChoiceFeatureBuilder, ChoiceFeatureBuilder> setupFeature) =>
        AddFeature(setupFeature(ChoiceFeatureBuilder.Create(name)).Build());

    public CatalogBuilder Number(string name, Func<NumberFeatureBuilder, NumberFeatureBuilder> setupFeature) =>
        AddFeature(setupFeature(NumberFeatureBuilder.Create(name)).Build());

    public CatalogBuilder Boolean(string name) =>
        AddFeature(new BooleanFeature(name));

    public CatalogBuilder Text(string name) =>
        AddFeature(new TextFeature(name));

    public CatalogBuilder Computed(string name, Expression expression) =>
        AddFeature(new ComputedFeature(name, expression));

    public CatalogBuilder Collection(string name, Func<CollectionBuilder, CollectionBuilder> setupCollection) =>
        _catalog.Collections.Any(c => c.Name == name)
            ? throw Duplicate("Collection", name)
            : new CatalogBuilder(_catalog with
            {
                Collections = _catalog.Collections.Add(setupCollection(CollectionBuilder.Create(name)).Build())
            });

    public CatalogBuilder Rule(string name, Func<RuleBuilder, RuleBuilder> setupRule) =>
        new(_catalog with
        {
            Rules = _catalog.Rules.Add(setupRule(RuleBuilder.Create(name)).Build())
        });

    public CatalogBuilder Table(string name, Table table) =>
        TableExists(name)
            ? throw Duplicate("Table", name)
            : new CatalogBuilder(_catalog with { Tables = _catalog.Tables.Add(name, table) });

    public CatalogBuilder AllowedCombinations(string name,
        Func<AllowedCombinationsTableBuilder, AllowedCombinationsTableBuilder> setupTable) =>
        TableExists(name)
            ? throw Duplicate("Table", name)
            : new CatalogBuilder(_catalog with
            {
                AllowedCombinations =
                _catalog.AllowedCombinations.Add(name, setupTable(AllowedCombinationsTableBuilder.Create()).Build())
            });

    private bool TableExists(string name) =>
        _catalog.Tables.ContainsKey(name) || _catalog.AllowedCombinations.ContainsKey(name);

    private static ArgumentException Duplicate(string kind, string name) =>
        new($"{kind} with name '{name}' already exists.");

    public Catalog Build() => _catalog;
}

public sealed record Catalog(
    string Name,
    ImmutableList<Feature> Features,
    ImmutableList<Collection> Collections,
    ImmutableList<Rule> Rules,
    ImmutableDictionary<string, Table> Tables,
    ImmutableDictionary<string, AllowedCombinationsTable> AllowedCombinations);

public abstract record Feature(string Name);

public sealed record ChoiceFeature(string Name, ImmutableList<FeatureOption> Options) : Feature(Name);

public sealed class ChoiceFeatureBuilder
{
    private readonly ChoiceFeature _feature;

    private ChoiceFeatureBuilder(ChoiceFeature feature) => _feature = feature;

    public static ChoiceFeatureBuilder Create(string name) => new(new ChoiceFeature(name, []));

    public ChoiceFeatureBuilder Option(
        string id,
        string name,
        Func<FeatureOptionBuilder, FeatureOptionBuilder>? setupOption = null)
    {
        var builder = FeatureOptionBuilder.Create(id, name);
        if (setupOption is not null) builder = setupOption(builder);
        return new ChoiceFeatureBuilder(_feature with { Options = _feature.Options.Add(builder.Build()) });
    }

    public ChoiceFeature Build() => _feature;
}

public sealed record FeatureOption(
    string Id,
    string Name,
    ImmutableDictionary<string, Value> Attributes,
    string Symbol);

public sealed class FeatureOptionBuilder
{
    private readonly FeatureOption _option;

    private FeatureOptionBuilder(FeatureOption option) => _option = option;

    public static FeatureOptionBuilder Create(string id, string name) =>
        new(new FeatureOption(id, name, ImmutableDictionary<string, Value>.Empty, id));

    public FeatureOptionBuilder Attribute(string key, Value value) =>
        new(_option with { Attributes = _option.Attributes.Add(key, value) });

    public FeatureOptionBuilder Symbol(string symbol) => new(_option with { Symbol = symbol });

    public FeatureOption Build() => _option;
}

public sealed record NumberFeature(
    string Name,
    decimal? Step,
    decimal? Min,
    decimal? Max,
    string? Unit) : Feature(Name);

public sealed class NumberFeatureBuilder
{
    private readonly NumberFeature _feature;

    private NumberFeatureBuilder(NumberFeature feature) => _feature = feature;

    public static NumberFeatureBuilder Create(string name) =>
        new(new NumberFeature(name, null, null, null, null));

    public NumberFeatureBuilder Step(decimal step) => new(_feature with { Step = step });
    public NumberFeatureBuilder Min(decimal min) => new(_feature with { Min = min });
    public NumberFeatureBuilder Max(decimal max) => new(_feature with { Max = max });
    public NumberFeatureBuilder Unit(string unit) => new(_feature with { Unit = unit });
    public NumberFeature Build() => _feature;
}

public sealed record BooleanFeature(string Name) : Feature(Name);

public sealed record TextFeature(string Name) : Feature(Name);

public sealed record ComputedFeature(string Name, Expression Expression) : Feature(Name);

public sealed record Collection(string Name, ImmutableList<Stage> Stages, CollectionMode Mode)
{
    public ImmutableList<string> Features => [.. Stages.SelectMany(s => s.Features)];
}

public sealed record Stage(string Name, ImmutableList<string> Features);

public enum CollectionMode
{
    Sequential,
    Independent
}

public sealed class CollectionBuilder
{
    private readonly Collection _collection;

    private CollectionBuilder(Collection collection) => _collection = collection;

    public static CollectionBuilder Create(string name) =>
        new(new Collection(name, ImmutableList<Stage>.Empty, CollectionMode.Sequential));

    public CollectionBuilder Stage(string stageName, string feature, params IEnumerable<string> features)
        => new(_collection with
        {
            Stages = _collection.Stages.Add(new Stage(stageName, [.. features.Prepend(feature)]))
        });

    public CollectionBuilder Independent()
        => new(_collection with { Mode = CollectionMode.Independent });

    public Collection Build() => _collection;
}

public sealed record Rule(
    string Name,
    int Priority,
    ImmutableList<string> Collections,
    Expression? Condition,
    ImmutableList<Effect> Effects,
    Violation? Violation);

public sealed class RuleBuilder
{
    private readonly Rule _rule;

    private RuleBuilder(Rule rule) => _rule = rule;

    public static RuleBuilder Create(string name) =>
        new(new Rule(name, 0, ImmutableList<string>.Empty, null, ImmutableList<Effect>.Empty, null));

    public RuleBuilder When(Expression condition) => new(_rule with { Condition = condition });

    public RuleBuilder Priority(int priority) => new(_rule with { Priority = priority });

    public RuleBuilder AppliesTo(string collection, params IEnumerable<string> collections) =>
        new(_rule with { Collections = _rule.Collections.Add(collection).AddRange(collections) });

    private RuleBuilder AddEffect(Effect effect) => new(_rule with { Effects = _rule.Effects.Add(effect) });

    public RuleBuilder Default(string feature, Expression value) => AddEffect(new DefaultEffect(feature, value));

    public RuleBuilder Set(string feature, Expression value) => AddEffect(new SetEffect(feature, value));

    public RuleBuilder Hide(string feature, params IEnumerable<string> features) =>
        AddEffect(new HideEffect([feature, .. features]));

    public RuleBuilder Lock(string feature, params IEnumerable<string> features) =>
        AddEffect(new LockEffect([feature, .. features]));

    public RuleBuilder Only(string feature, string option, params IEnumerable<string> options) =>
        AddEffect(new OnlyEffect(feature, [Value.Option(option), .. options.Select(Value.Option)]));

    public RuleBuilder Forbid(string feature, string option, params IEnumerable<string> options) =>
        AddEffect(new ForbidEffect(feature, [Value.Option(option), .. options.Select(Value.Option)]));

    public RuleBuilder Min(string feature, Expression value) => AddEffect(new MinEffect(feature, value));

    public RuleBuilder Max(string feature, Expression value) => AddEffect(new MaxEffect(feature, value));

    public RuleBuilder Step(string feature, Expression value) => AddEffect(new StepEffect(feature, value));

    public RuleBuilder DefaultsFrom(string table, string feature, params IEnumerable<string> features) =>
        AddEffect(new DefaultsFromTableEffect(table, [feature, .. features]));

    public RuleBuilder OnlyFrom(string feature, string table) => AddEffect(new OnlyFromTableEffect(feature, table));

    public RuleBuilder Inform(string message) => AddEffect(new MessageEffect(Severity.Information, message));
    public RuleBuilder Warn(string message) => AddEffect(new MessageEffect(Severity.Warning, message));
    public RuleBuilder Error(string message) => AddEffect(new MessageEffect(Severity.Error, message));

    public RuleBuilder OnViolation(Reaction reaction, string message) =>
        new(_rule with { Violation = new Violation(reaction, message) });

    public Rule Build() => _rule;
}

public abstract record Effect;

public sealed record DefaultEffect(string Feature, Expression Value) : Effect;

public sealed record SetEffect(string Feature, Expression Value) : Effect;

public sealed record HideEffect(ImmutableList<string> Features) : Effect;

public sealed record LockEffect(ImmutableList<string> Features) : Effect;

public sealed record OnlyEffect(string Feature, ImmutableList<Value> Options) : Effect;

public sealed record ForbidEffect(string Feature, ImmutableList<Value> Options) : Effect;

public sealed record MinEffect(string Feature, Expression Value) : Effect;

public sealed record MaxEffect(string Feature, Expression Value) : Effect;

public sealed record StepEffect(string Feature, Expression Value) : Effect;

public sealed record DefaultsFromTableEffect(string Table, ImmutableList<string> Features) : Effect;

public sealed record OnlyFromTableEffect(string Feature, string Table) : Effect;

public sealed record MessageEffect(Severity Severity, string Text) : Effect;

public enum Severity
{
    Information,
    Warning,
    Error,
}

public sealed record Violation(Reaction Reaction, string Message);

public enum Reaction
{
    Correct,
    Warn,
    Error,
}

public sealed record AllowedCombinationsTable(
    ImmutableList<string> KeyColumns,
    string AllowedFeature,
    ImmutableDictionary<ImmutableList<Value>, ImmutableList<Value>> Rows)
{
    public ImmutableList<Value>? AllowedFor(ImmutableList<Value> key) =>
        key.Any(k => k is MissingValue) ? null : Rows.GetValueOrDefault(key, []);
}

internal sealed class SequenceComparer : IEqualityComparer<ImmutableList<Value>>
{
    public static readonly SequenceComparer Instance = new();
    public bool Equals(ImmutableList<Value>? x, ImmutableList<Value>? y) => x!.SequenceEqual(y!);

    public int GetHashCode(ImmutableList<Value> key) =>
        key.Aggregate(new HashCode(), (h, v) =>
        {
            h.Add(v);
            return h;
        }).ToHashCode();
}

public sealed class AllowedCombinationsTableBuilder
{
    private readonly AllowedCombinationsTable _table;

    private AllowedCombinationsTableBuilder(AllowedCombinationsTable table) => _table = table;

    public static AllowedCombinationsTableBuilder Create() =>
        new(new AllowedCombinationsTable(
            ImmutableList<string>.Empty,
            string.Empty,
            ImmutableDictionary.Create<ImmutableList<Value>, ImmutableList<Value>>(SequenceComparer.Instance)));

    public AllowedCombinationsTableBuilder Key(string key, params IEnumerable<string> keys) =>
        new(_table with { KeyColumns = _table.KeyColumns.Add(key).AddRange(keys) });

    public AllowedCombinationsTableBuilder Allowed(string feature) =>
        new(_table with { AllowedFeature = feature });

    public AllowedCombinationsTableBuilder Row(string cell, params IReadOnlyList<string> cells)
    {
        string[] all = [cell, .. cells];
        if (all.Length - _table.KeyColumns.Count != 1)
        {
            throw new ArgumentException(
                $"Row for '{_table.AllowedFeature}' needs {_table.KeyColumns.Count + 1} cells (keys + allowed option), got {all.Length}.");
        }

        ImmutableList<Value> key = [.. all.SkipLast(1).Select(Value.Option)];
        var value = Value.Option(all[^1]);
        var allowed = _table.Rows.GetValueOrDefault(key, []);
        return new AllowedCombinationsTableBuilder(_table with { Rows = _table.Rows.SetItem(key, allowed.Add(value)) });
    }

    public AllowedCombinationsTable Build() => _table;
}
