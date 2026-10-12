using F.Configurator.Catalog;

namespace F.Configurator.Engine;

public sealed record RuleViolation(string Feature, string? RuleName, Reaction Reaction, string? Message);
