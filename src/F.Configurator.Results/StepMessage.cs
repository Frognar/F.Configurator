namespace F.Configurator.Results;

public sealed record StepMessage(MessageKind Kind, string? Text, string? RuleName, string? Feature);
