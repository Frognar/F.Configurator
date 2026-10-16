namespace F.Configurator.Dsl;

public sealed record LexResult(IReadOnlyList<Token> Tokens, IReadOnlyList<Diagnostic> Diagnostics);
