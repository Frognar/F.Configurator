using F.Configurator.Expressions;

namespace F.Configurator.Results;

public sealed record FeatureResult(
    string Name,
    Value Value,
    string? Symbol,
    ValueSource? Source,
    bool IsVisible,
    EquatableDictionary<string, Value> Attributes);
