using F.Configurator.Catalog;

namespace F.Configurator.Engine;

public sealed record RuleMessage(string RuleName, Severity Severity, string Text);
