using F.Configurator.Expressions;

namespace F.Configurator.StepResult;

public sealed record StepOutcome(
    string Catalog,
    string? Version,
    string Collection,
    EquatableList<FeatureOutcome> Features,
    EquatableList<StepMessage> Messages,
    bool IsComplete);
