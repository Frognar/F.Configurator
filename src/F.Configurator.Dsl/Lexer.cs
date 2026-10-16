namespace F.Configurator.Dsl;

public static class Lexer
{
    public static LexResult Tokenize(string file, string source)
    {
        List<Token> tokens = [];
        var lines = source.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var number = index + 1;
            var count = tokens.Count;
            var column = 0;
            while (column < line.Length)
            {
                if (line[column] == ' ')
                {
                    column++;
                    continue;
                }

                var start = column;
                while (column < line.Length && line[column] != ' ')
                {
                    column++;
                }

                tokens.Add(new Token(
                    TokenKind.Name, line[start..column], new SourceSpan(file, number, start + 1, column - start)));
            }

            if (tokens.Count > count)
            {
                tokens.Add(new Token(TokenKind.NewLine, "", new SourceSpan(file, number, line.Length + 1, 0)));
            }
        }

        tokens.Add(new Token(TokenKind.End, "", new SourceSpan(file, lines.Length, 1, 0)));
        return new LexResult(tokens, []);
    }
}
