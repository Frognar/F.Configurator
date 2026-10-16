namespace F.Configurator.Dsl;

public static class Lexer
{
    public static LexResult Tokenize(string file, string source) =>
        new([new Token(TokenKind.End, "", new SourceSpan(file, 1, 1, 0))], []);
}
