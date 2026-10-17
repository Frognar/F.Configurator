using System.Globalization;
using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl.Tests;

internal static class Parsing
{
    public const string File = "kolekcje/basic.kfg";

    public static ParseResult<ExpressionSyntax> ParseExpression(string source) => Parser.ParseExpression(File, source);

    // The tree written as nested lists, operator first, so a test shows its structure in one line:
    // `A + B * 2` is `(+ A (* B 2))`.
    public static string Tree(string source)
    {
        var result = ParseExpression(source);
        Assert.Empty(result.Diagnostics);
        return Print(result.Syntax!);
    }

    public static SourceSpan At(int line, int column, int length) => new(File, line, column, length);

    private static string Print(ExpressionSyntax syntax) =>
        syntax switch
        {
            NumberSyntax number => number.Value.ToString(CultureInfo.InvariantCulture),
            TextSyntax text => $"\"{text.Value}\"",
            NameSyntax name => name.Name,
            MemberSyntax member => $"(. {Print(member.Target)} {member.Member})",
            LookupSyntax lookup => $"([] {lookup.Table} {string.Join(" ", lookup.Keys.Select(Print))})",
            CallSyntax call => $"({call.Function} {string.Join(" ", call.Arguments.Select(Print))})",
            ListSyntax list => $"[{string.Join(" ", list.Items.Select(Print))}]",
            UnarySyntax unary => $"({unary.Operator} {Print(unary.Operand)})",
            BinarySyntax binary => $"({binary.Operator} {Print(binary.Left)} {Print(binary.Right)})",
            ConditionalSyntax conditional =>
                $"(jeśli {Print(conditional.Condition)} {Print(conditional.Then)} {Print(conditional.Otherwise)})",
            _ => throw new ArgumentOutOfRangeException(nameof(syntax), syntax, null),
        };
}
