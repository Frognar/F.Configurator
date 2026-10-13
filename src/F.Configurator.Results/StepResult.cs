using F.Configurator.Expressions;

namespace F.Configurator.Results;

public sealed record StepResult(
    string Catalog,
    string? Version,
    string Collection,
    EquatableList<FeatureResult> Features,
    EquatableList<StepMessage> Messages,
    bool IsComplete);
