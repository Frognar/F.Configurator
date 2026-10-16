namespace F.Configurator.Dsl;

public sealed record Diagnostic(Severity Severity, SourceSpan Span, string Message, string? Hint);
