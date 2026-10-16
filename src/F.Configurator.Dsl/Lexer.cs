using System.Text;

namespace F.Configurator.Dsl;

public static class Lexer
{
    private static readonly string[] TwoCharacterOperators = ["..", "<=", ">=", "!="];

    // Grammar 6.1: other ways to write an operator.
    private static readonly Dictionary<string, string> OtherSpellings = new()
    {
        ["!="] = "≠",
        ["≤"] = "<=",
        ["≥"] = ">=",
    };

    public static LexResult Tokenize(string file, string source)
    {
        List<Token> tokens = [];
        Stack<int> levels = new([0]);
        var lines = source.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var number = index + 1;
            var count = tokens.Count;
            var column = line.Length - line.TrimStart(' ').Length;
            if (column < line.Length && line[column] != '#' && column > levels.Peek() && (GoesOn(tokens) || GoesOnFromAbove(line, column)))
            {
                tokens.RemoveAt(tokens.Count - 1);
                count--;
            }
            else if (column < line.Length && line[column] != '#' && column > levels.Peek())
            {
                levels.Push(column);
                tokens.Add(new Token(TokenKind.Indent, "", new SourceSpan(file, number, 1, column)));
                count++;
            }

            for (; column < line.Length && line[column] != '#' && column < levels.Peek(); levels.Pop())
            {
                tokens.Add(new Token(TokenKind.Dedent, "", new SourceSpan(file, number, column + 1, 0)));
                count++;
            }

            while (column < line.Length)
            {
                var start = column;
                var current = line[column];
                if (current == ' ')
                {
                    column++;
                }
                else if (current == '#')
                {
                    break;
                }
                else if (char.IsLetter(current))
                {
                    column = WordEnd(line, column);
                    var word = line[start..column];
                    var keyword = Keywords.KeywordOf(word);
                    tokens.Add(new Token(
                        keyword is null ? TokenKind.Name : TokenKind.Keyword,
                        keyword ?? word,
                        new SourceSpan(file, number, start + 1, column - start)));
                }
                else if (char.IsAsciiDigit(current))
                {
                    column = NumberEnd(line, column);
                    tokens.Add(new Token(
                        TokenKind.Number, line[start..column], new SourceSpan(file, number, start + 1, column - start)));
                }
                else if (current == '"')
                {
                    var content = new StringBuilder();
                    column++;
                    while (line[column] != '"')
                    {
                        if (line[column] == '\\' && column + 1 < line.Length && line[column + 1] == '"')
                        {
                            column++;
                        }

                        content.Append(line[column]);
                        column++;
                    }

                    column++;
                    tokens.Add(new Token(
                        TokenKind.Text,
                        content.ToString(),
                        new SourceSpan(file, number, start + 1, column - start)));
                }
                else
                {
                    column += TwoCharacterOperators.Any(o => line[column..].StartsWith(o, StringComparison.Ordinal)) ? 2 : 1;
                    var written = line[start..column];
                    tokens.Add(new Token(
                        TokenKind.Operator,
                        OtherSpellings.GetValueOrDefault(written, written),
                        new SourceSpan(file, number, start + 1, column - start)));
                }
            }

            if (tokens.Count > count)
            {
                tokens.Add(new Token(TokenKind.NewLine, "", new SourceSpan(file, number, line.Length + 1, 0)));
            }
        }

        for (; levels.Peek() > 0; levels.Pop())
        {
            tokens.Add(new Token(TokenKind.Dedent, "", new SourceSpan(file, lines.Length, 1, 0)));
        }

        tokens.Add(new Token(TokenKind.End, "", new SourceSpan(file, lines.Length, 1, 0)));
        return new LexResult(tokens, []);
    }

    // Grammar 3.1: a long line goes on in the next, deeper line after a comma or an operator that needs a right side.
    private static bool GoesOn(List<Token> tokens) =>
        tokens is [.., { Kind: TokenKind.Operator, Text: "," or "=" or "+" or "-" or "*" or "/" }, { Kind: TokenKind.NewLine }]
            or [.., { Kind: TokenKind.Keyword, Text: "i" or "lub" }, { Kind: TokenKind.NewLine }];

    // A deeper line that starts with `i`, `lub` or an arithmetic operator goes on from the line above.
    private static bool GoesOnFromAbove(string line, int column) =>
        line[column] is '+' or '-' or '*' or '/' || line[column..WordEnd(line, column)] is "i" or "lub";

    // Digits, and a decimal part after a dot followed by a digit.
    private static int NumberEnd(string line, int column)
    {
        column = DigitsEnd(line, column);
        return column + 1 < line.Length && line[column] == '.' && char.IsAsciiDigit(line[column + 1])
            ? DigitsEnd(line, column + 1)
            : column;
    }

    private static int DigitsEnd(string line, int column)
    {
        while (column < line.Length && char.IsAsciiDigit(line[column]))
        {
            column++;
        }

        return column;
    }

    // A word is letters, digits and underscores; `tak/nie` is one word, the name of the yes/no type.
    private static int WordEnd(string line, int column)
    {
        var start = column;
        while (column < line.Length && (char.IsLetterOrDigit(line[column]) || line[column] == '_'))
        {
            column++;
        }

        return line[start..column] == "tak" && line[column..].StartsWith("/nie", StringComparison.Ordinal)
            ? column + "/nie".Length
            : column;
    }
}
