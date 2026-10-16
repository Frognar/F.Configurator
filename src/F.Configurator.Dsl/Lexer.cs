namespace F.Configurator.Dsl;

public static class Lexer
{
    public static LexResult Tokenize(string file, string source)
    {
        List<Token> tokens = [];
        var column = 0;
        while (column < source.Length)
        {
            if (source[column] == ' ')
            {
                column++;
                continue;
            }

            var start = column;
            while (column < source.Length && source[column] != ' ')
            {
                column++;
            }

            tokens.Add(new Token(TokenKind.Name, source[start..column], new SourceSpan(file, 1, start + 1, column - start)));
        }

        if (tokens.Count > 0)
        {
            tokens.Add(new Token(TokenKind.NewLine, "", new SourceSpan(file, 1, source.Length + 1, 0)));
        }

        tokens.Add(new Token(TokenKind.End, "", new SourceSpan(file, 1, 1, 0)));
        return new LexResult(tokens, []);
    }
}
