using System.Text;

namespace F.Configurator.Dsl;

public static class Lexer
{
    private const string SingleCharacterOperators = "=≠<>≤≥+-*/()[],:.|%";

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
        var lines = source.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        var output = new Output(file);
        Stack<int> levels = new([0]);
        for (var index = 0; index < lines.Length; index++)
        {
            var line = new Line(lines[index], index + 1);
            var indentation = line.Text.Length - line.Text.TrimStart(' ').Length;
            if (indentation < line.Text.Length && line.Text[indentation] != '#')
            {
                Indentation(output, levels, line, indentation);
                Scan(output, line, indentation);
                output.Add(TokenKind.NewLine, "", line.Number, line.Text.Length, 0);
            }
        }

        for (; levels.Peek() > 0; levels.Pop())
        {
            output.Add(TokenKind.Dedent, "", lines.Length, 0, 0);
        }

        output.Add(TokenKind.End, "", lines.Length, 0, 0);
        return new LexResult(output.Tokens, output.Diagnostics);
    }

    // Grammar 3.1: a deeper line either goes on from the line above or opens a block; a shallower one closes blocks.
    private static void Indentation(Output output, Stack<int> levels, Line line, int indentation)
    {
        if (output.Tokens.Count > 0
            && ((indentation > levels.Peek() && GoesOn(output.Tokens))
                || (indentation >= levels.Peek() && GoesOnFromAbove(line.Text, indentation))))
        {
            output.Tokens.RemoveAt(output.Tokens.Count - 1);
        }
        else if (indentation > levels.Peek())
        {
            levels.Push(indentation);
            output.Add(TokenKind.Indent, "", line.Number, 0, indentation);
        }
        else
        {
            for (; indentation < levels.Peek(); levels.Pop())
            {
                output.Add(TokenKind.Dedent, "", line.Number, indentation, 0);
            }

            if (indentation != levels.Peek())
            {
                output.Error(line.Number, 0, indentation, "wcięcie nie pasuje do żadnego z poprzednich poziomów");
            }
        }
    }

    private static void Scan(Output output, Line line, int column)
    {
        var text = line.Text;
        while (column < text.Length && text[column] != '#')
        {
            var current = text[column];
            column = current switch
            {
                ' ' => column + 1,
                '\t' => output.Error(line.Number, column, 1, "tabulator we wcięciu, użyj spacji"),
                '"' => ScanText(output, line, column),
                _ when char.IsLetter(current) => ScanWord(output, line, column),
                _ when char.IsAsciiDigit(current) => ScanNumber(output, line, column),
                _ when OperatorLength(text, column) is var length and > 0 => ScanOperator(output, line, column, length),
                _ => output.Error(line.Number, column, 1, $"nieznany znak „{current}”"),
            };
        }
    }

    private static int ScanWord(Output output, Line line, int start)
    {
        var end = WordEnd(line.Text, start);
        var word = line.Text[start..end];
        var keyword = Keywords.KeywordOf(word);
        return output.Add(
            keyword is null ? TokenKind.Name : TokenKind.Keyword, keyword ?? word, line.Number, start, end - start);
    }

    private static int ScanNumber(Output output, Line line, int start)
    {
        var text = line.Text;
        var end = NumberEnd(text, start);
        if (end + 1 < text.Length && text[end] == ',' && char.IsAsciiDigit(text[end + 1]))
        {
            var fraction = DigitsEnd(text, end + 1);
            output.Error(
                line.Number,
                start,
                fraction - start,
                $"liczba „{text[start..fraction]}” – użyj kropki",
                $"{text[start..end]}.{text[(end + 1)..fraction]}");
        }

        return output.Add(TokenKind.Number, text[start..end], line.Number, start, end - start);
    }

    // Grammar 3.3: a text ends at the next quote that is not escaped with `\`, or at the end of the line.
    private static int ScanText(Output output, Line line, int start)
    {
        var text = line.Text;
        var content = new StringBuilder();
        var column = start + 1;
        for (; column < text.Length && text[column] != '"'; column++)
        {
            if (text[column] == '\\' && column + 1 < text.Length && text[column + 1] == '"')
            {
                column++;
            }

            content.Append(text[column]);
        }

        if (column == text.Length)
        {
            output.Error(line.Number, start, column - start, "tekst bez cudzysłowu zamykającego");
        }

        var end = Math.Min(column + 1, text.Length);
        return output.Add(TokenKind.Text, content.ToString(), line.Number, start, end - start);
    }

    private static int ScanOperator(Output output, Line line, int start, int length)
    {
        var written = line.Text.Substring(start, length);
        return output.Add(
            TokenKind.Operator, OtherSpellings.GetValueOrDefault(written, written), line.Number, start, length);
    }

    private static int OperatorLength(string text, int column) =>
        TwoCharacterOperators.Any(o => text[column..].StartsWith(o, StringComparison.Ordinal)) ? 2
        : SingleCharacterOperators.Contains(text[column]) ? 1
        : 0;

    // Grammar 3.1: a long line goes on in the next, deeper line after a comma or an operator that needs a right side.
    private static bool GoesOn(List<Token> tokens) =>
        tokens is [.., { Kind: TokenKind.Operator, Text: "," or "=" or "+" or "-" or "*" or "/" }, { Kind: TokenKind.NewLine }]
            or [.., { Kind: TokenKind.Keyword, Text: "i" or "lub" }, { Kind: TokenKind.NewLine }];

    // A line not shallower than the block that starts with `i`, `lub`, `to`, `inaczej` or an arithmetic operator goes
    // on from the line above.
    private static bool GoesOnFromAbove(string text, int column) =>
        text[column] is '+' or '-' or '*' or '/'
        || text[column..WordEnd(text, column)] is "i" or "lub" or "to" or "inaczej";

    // Digits, and a decimal part after a dot followed by a digit.
    private static int NumberEnd(string text, int column)
    {
        column = DigitsEnd(text, column);
        return column + 1 < text.Length && text[column] == '.' && char.IsAsciiDigit(text[column + 1])
            ? DigitsEnd(text, column + 1)
            : column;
    }

    private static int DigitsEnd(string text, int column)
    {
        while (column < text.Length && char.IsAsciiDigit(text[column]))
        {
            column++;
        }

        return column;
    }

    // A word is letters, digits and underscores; `tak/nie` is one word, the name of the yes/no type.
    private static int WordEnd(string text, int column)
    {
        var start = column;
        while (column < text.Length && (char.IsLetterOrDigit(text[column]) || text[column] == '_'))
        {
            column++;
        }

        return text[start..column] == "tak" && text[column..].StartsWith("/nie", StringComparison.Ordinal)
            ? column + "/nie".Length
            : column;
    }

    private sealed record Line(string Text, int Number);

    // Collects tokens and diagnostics; columns are counted from 0 here and from 1 in spans.
    private sealed class Output(string file)
    {
        public List<Token> Tokens { get; } = [];

        public List<Diagnostic> Diagnostics { get; } = [];

        // Returns the column after the token, so scanning goes on from there.
        public int Add(TokenKind kind, string text, int line, int column, int length)
        {
            Tokens.Add(new Token(kind, text, new SourceSpan(file, line, column + 1, length)));
            return column + length;
        }

        // Returns the column after the problem, so scanning skips it.
        public int Error(int line, int column, int length, string message, string? hint = null)
        {
            Diagnostics.Add(new Diagnostic(Severity.Error, new SourceSpan(file, line, column + 1, length), message, hint));
            return column + length;
        }
    }
}
