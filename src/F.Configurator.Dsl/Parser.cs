using System.Globalization;
using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static class Parser
{
    public static ParseResult<ExpressionSyntax> ParseExpression(string file, string source)
    {
        var lexed = Lexer.Tokenize(file, source);
        var reader = new Reader(lexed.Tokens);
        return new ParseResult<ExpressionSyntax>(reader.Expression(), lexed.Diagnostics);
    }

    private sealed class Reader(IReadOnlyList<Token> tokens)
    {
        private int _position;

        private Token Current => tokens[_position];

        public ExpressionSyntax Expression() => Additive();

        private ExpressionSyntax Additive() => LeftToRight(Multiplicative, "+", "-");

        private ExpressionSyntax Multiplicative() => LeftToRight(Postfix, "*", "/");

        private ExpressionSyntax LeftToRight(Func<ExpressionSyntax> operand, params string[] operators)
        {
            var syntax = operand();
            while (Current.Kind == TokenKind.Operator && operators.Contains(Current.Text))
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
                var member = Next();
                syntax = new MemberSyntax(syntax, member.Text, Cover(syntax.Span, member.Span));
            }

            return syntax;
        }

        private ExpressionSyntax Primary()
        {
            var token = Next();
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
                _ => new NumberSyntax(decimal.Parse(token.Text, CultureInfo.InvariantCulture), token.Span),
            };
        }

        private Token Next() => tokens[_position++];

        // From the start of the first span to the end of the last one, when both are on one line.
        private static SourceSpan Cover(SourceSpan first, SourceSpan last) =>
            first.Line == last.Line ? first with { Length = last.Column + last.Length - first.Column } : first;
    }
}
