using System.Globalization;
using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static class Parser
{
    public static ParseResult<ExpressionSyntax> ParseExpression(string file, string source)
    {
        var lexed = Lexer.Tokenize(file, source);
        var token = lexed.Tokens[0];
        return new ParseResult<ExpressionSyntax>(
            new NumberSyntax(decimal.Parse(token.Text, CultureInfo.InvariantCulture), token.Span), lexed.Diagnostics);
    }
}
