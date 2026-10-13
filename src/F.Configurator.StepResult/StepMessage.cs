namespace F.Configurator.StepResult;

public sealed record StepMessage(MessageKind Kind, string? Text, string? RuleName, string? Feature);
