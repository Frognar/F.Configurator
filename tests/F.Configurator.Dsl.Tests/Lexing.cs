namespace F.Configurator.Dsl.Tests;

internal static class Lexing
{
    public const string File = "kolekcje/basic.kfg";

    public static readonly (TokenKind, string) NewLine = (TokenKind.NewLine, "");
    public static readonly (TokenKind, string) Indent = (TokenKind.Indent, "");
    public static readonly (TokenKind, string) Dedent = (TokenKind.Dedent, "");
    public static readonly (TokenKind, string) End = (TokenKind.End, "");

    public static LexResult Lex(string source) => Lexer.Tokenize(File, source);

    // Kind and text of each token; spans have their own tests.
    public static IEnumerable<(TokenKind Kind, string Text)> Tokens(string source) =>
        Lex(source).Tokens.Select(token => (token.Kind, token.Text));

    public static (TokenKind, string) Name(string text) => (TokenKind.Name, text);

    public static (TokenKind, string) Keyword(string text) => (TokenKind.Keyword, text);

    public static (TokenKind, string) Number(string text) => (TokenKind.Number, text);

    public static (TokenKind, string) Text(string text) => (TokenKind.Text, text);

    public static (TokenKind, string) Operator(string text) => (TokenKind.Operator, text);

    public static SourceSpan At(int line, int column, int length) => new(File, line, column, length);
}
