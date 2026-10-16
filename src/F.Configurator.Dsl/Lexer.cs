namespace F.Configurator.Dsl;

public static class Lexer
{
    public static LexResult Tokenize(string file, string source)
    {
        List<Token> tokens = [];
        if (source != "")
        {
            tokens.Add(new Token(TokenKind.Name, source, new SourceSpan(file, 1, 1, source.Length)));
            tokens.Add(new Token(TokenKind.NewLine, "", new SourceSpan(file, 1, source.Length + 1, 0)));
        }

        tokens.Add(new Token(TokenKind.End, "", new SourceSpan(file, 1, 1, 0)));
        return new LexResult(tokens, []);
    }
}
