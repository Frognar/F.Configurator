namespace F.Configurator.Dsl;

public sealed record Token(TokenKind Kind, string Text, SourceSpan Span);
