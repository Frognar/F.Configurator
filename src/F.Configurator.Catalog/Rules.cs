using F.Configurator.Expressions;

namespace F.Configurator.Catalog;

public sealed record Rule(
    string Name,
    int Priority,
    EquatableList<string> Collections,
    Expression? Condition,
    EquatableList<Effect> Effects,
    Violation? Violation);

public sealed class RuleBuilder
{
    private readonly Rule _rule;

    private RuleBuilder(Rule rule) => _rule = rule;

    public static RuleBuilder Create(string name) =>
        new(new Rule(name, 0, EquatableList<string>.Empty, null, EquatableList<Effect>.Empty, null));

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

    public RuleBuilder DefaultsFrom(IEnumerable<string> features, string table) =>
        AddEffect(new DefaultsFromTableEffect([.. features], table));

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

public sealed record HideEffect(EquatableList<string> Features) : Effect;

public sealed record LockEffect(EquatableList<string> Features) : Effect;

public sealed record OnlyEffect(string Feature, EquatableList<Value> Options) : Effect;

public sealed record ForbidEffect(string Feature, EquatableList<Value> Options) : Effect;

public sealed record MinEffect(string Feature, Expression Value) : Effect;

public sealed record MaxEffect(string Feature, Expression Value) : Effect;

public sealed record StepEffect(string Feature, Expression Value) : Effect;

public sealed record DefaultsFromTableEffect(EquatableList<string> Features, string Table) : Effect;

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
    Warning,
    Error,
}
