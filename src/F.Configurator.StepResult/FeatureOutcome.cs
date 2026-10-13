using F.Configurator.Expressions;

namespace F.Configurator.StepResult;

public sealed record FeatureOutcome(
    string Name,
    Value Value,
    string? Symbol,
    ValueSource? Source,
    bool IsVisible,
    EquatableDictionary<string, Value> Attributes);
