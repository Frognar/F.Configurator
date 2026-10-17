using System.Globalization;
using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static class Parser
{
    public static ParseResult<ExpressionSyntax> ParseExpression(string file, string source)
    {
        var lexed = Lexer.Tokenize(file, source);
        var token = lexed.Tokens[0];
        ExpressionSyntax syntax = token.Kind switch
        {
            TokenKind.Text => new TextSyntax(token.Text, token.Span),
            _ => new NumberSyntax(decimal.Parse(token.Text, CultureInfo.InvariantCulture), token.Span),
        };
        return new ParseResult<ExpressionSyntax>(syntax, lexed.Diagnostics);
    }
}
