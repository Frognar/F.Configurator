using F.Configurator.Expressions;

namespace F.Configurator.Results;

public sealed record ValueChange(string Feature, Value Before, Value After, ChangeCause Cause, string? RuleName);
