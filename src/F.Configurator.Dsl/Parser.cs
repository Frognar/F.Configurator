using System.Globalization;
using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static class Parser
{
    public static ParseResult<ExpressionSyntax> ParseExpression(string file, string source)
    {
        var lexed = Lexer.Tokenize(file, source);
        var reader = new Reader(lexed.Tokens);
        try
        {
            return new ParseResult<ExpressionSyntax>(reader.Expression(), lexed.Diagnostics);
        }
        catch (SyntaxError error)
        {
            return new ParseResult<ExpressionSyntax>(null, [.. lexed.Diagnostics, error.Diagnostic]);
        }
    }

    // Grammar 6.3: the built-in functions.
    private static readonly string[] Functions =
        ["domyślna", "wybrana", "dopełnij", "zaokrąglij", "min", "maks", "długość"];

    private sealed class Reader(IReadOnlyList<Token> tokens)
    {
        private int _position;

        private Token Current => tokens[_position];

        public ExpressionSyntax Expression()
        {
            if (Current is not { Kind: TokenKind.Keyword, Text: "jeśli" })
            {
                return Or();
            }

            var @if = Next();
            var condition = Expression();
            Expect("to", "brak „to”");
            var then = Expression();
            Expect("inaczej", "brak „inaczej”");
            var otherwise = Expression();
            return new ConditionalSyntax(condition, then, otherwise, Cover(@if.Span, otherwise.Span));
        }

        private ExpressionSyntax Or() => LeftToRight(And, "lub");

        private ExpressionSyntax And() => LeftToRight(Not, "i");

        private ExpressionSyntax Not()
        {
            if (Current is not { Kind: TokenKind.Keyword, Text: "nie" })
            {
                return Comparison();
            }

            var not = Next();
            var operand = Not();
            return new UnarySyntax("nie", operand, Cover(not.Span, operand.Span));
        }

        private ExpressionSyntax Comparison()
        {
            var left = Range();
            if (Current is not ({ Kind: TokenKind.Operator, Text: "=" or "≠" or "<" or "<=" or ">" or ">=" }
                or { Kind: TokenKind.Keyword, Text: "w" }) && !NotIn())
            {
                return left;
            }

            var @operator = Next().Text;
            if (@operator == "nie")
            {
                _position++;
                @operator = "nie w";
            }

            var right = Range();
            return new BinarySyntax(@operator, left, right, Cover(left.Span, right.Span));
        }

        private ExpressionSyntax Range()
        {
            var min = Additive();
            if (Current is not { Kind: TokenKind.Operator, Text: ".." })
            {
                return min;
            }

            _position++;
            var max = Additive();
            return new BinarySyntax("..", min, max, Cover(min.Span, max.Span));
        }

        private bool NotIn() =>
            Current is { Kind: TokenKind.Keyword, Text: "nie" } && tokens[_position + 1] is { Kind: TokenKind.Keyword, Text: "w" };

        private ExpressionSyntax Additive() => LeftToRight(Multiplicative, "+", "-");

        private ExpressionSyntax Multiplicative() => LeftToRight(Negation, "*", "/");

        private ExpressionSyntax Negation()
        {
            if (Current is not { Kind: TokenKind.Operator, Text: "-" })
            {
                return Postfix();
            }

            var minus = Next();
            var operand = Negation();
            return new UnarySyntax("-", operand, Cover(minus.Span, operand.Span));
        }

        private ExpressionSyntax LeftToRight(Func<ExpressionSyntax> operand, params string[] operators)
        {
            var syntax = operand();
            while (Current.Kind is TokenKind.Operator or TokenKind.Keyword && operators.Contains(Current.Text))
            {
                var @operator = Next().Text;
                var right = operand();
                syntax = new BinarySyntax(@operator, syntax, right, Cover(syntax.Span, right.Span));
            }

            return syntax;
        }

        private ExpressionSyntax Postfix()
        {
            var syntax = Primary();
            while (Current is { Kind: TokenKind.Operator, Text: "." })
            {
                _position++;
                var member = Current.Kind == TokenKind.Name
                    ? Next()
                    : throw new SyntaxError(Current.Span, "po „.” oczekiwano nazwy");
                syntax = new MemberSyntax(syntax, member.Text, Cover(syntax.Span, member.Span));
            }

            return syntax;
        }

        private ExpressionSyntax Primary()
        {
            var token = Next();
            if (token is { Kind: TokenKind.Operator, Text: "(" })
            {
                var inner = Expression();
                var close = Expect(")", "brak „)”");
                return inner with { Span = Cover(token.Span, close.Span) };
            }

            if (token is { Kind: TokenKind.Operator, Text: "[" })
            {
                List<ExpressionSyntax> items = [Expression()];
                while (Current is { Kind: TokenKind.Operator, Text: "," })
                {
                    _position++;
                    items.Add(Expression());
                }

                var close = Expect("]", "brak „]”");
                return new ListSyntax(items, Cover(token.Span, close.Span));
            }

            if (token is { Kind: TokenKind.Keyword, Text: var function } && Functions.Contains(function))
            {
                Expect("(", $"po „{function}” oczekiwano „(”");
                List<ExpressionSyntax> arguments = [Expression()];
                while (Current is { Kind: TokenKind.Operator, Text: "," })
                {
                    _position++;
                    arguments.Add(Expression());
                }

                var close = Expect(")", "brak „)”");
                return new CallSyntax(function, arguments, Cover(token.Span, close.Span));
            }

            if (token.Kind == TokenKind.Name && Current is { Kind: TokenKind.Operator, Text: "[" })
            {
                _position++;
                List<ExpressionSyntax> keys = [Postfix()];
                while (Current is { Kind: TokenKind.Operator, Text: "," })
                {
                    _position++;
                    keys.Add(Postfix());
                }

                var close = Next();
                return new LookupSyntax(token.Text, keys, Cover(token.Span, close.Span));
            }

            return token.Kind switch
            {
                TokenKind.Text => new TextSyntax(token.Text, token.Span),
                TokenKind.Name => new NameSyntax(token.Text, token.Span),
                TokenKind.Number => new NumberSyntax(decimal.Parse(token.Text, CultureInfo.InvariantCulture), token.Span),
                _ => throw new SyntaxError(token.Span, "oczekiwano wyrażenia"),
            };
        }

        private Token Next() => tokens[_position++];

        private Token Expect(string text, string message) =>
            Current.Kind is TokenKind.Operator or TokenKind.Keyword && Current.Text == text
                ? Next()
                : throw new SyntaxError(Current.Span, message);

        // From the start of the first span to the end of the last one, when both are on one line.
        private static SourceSpan Cover(SourceSpan first, SourceSpan last) =>
            first.Line == last.Line ? first with { Length = last.Column + last.Length - first.Column } : first;
    }

    // Stops reading at the first problem; the expression then has no tree.
    private sealed class SyntaxError(SourceSpan span, string message) : Exception(message)
    {
        public Diagnostic Diagnostic { get; } = new(Severity.Error, span, message, null);
    }
}
