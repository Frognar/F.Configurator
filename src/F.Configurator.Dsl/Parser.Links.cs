using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static partial class Parser
{
    private sealed partial class Reader
    {
        private LinkSyntax Link()
        {
            var name = Name();
            EndOfLine();
            string? from = null;
            string? to = null;
            ExpressionSyntax? proposeWhen = null;
            List<TakeoverSyntax> takeovers = [];
            Block(() =>
            {
                if (Current is { Kind: TokenKind.Keyword, Text: "przejmij" })
                {
                    takeovers.Add(Takeover());
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "proponuj" })
                {
                    _position++;
                    Expect("gdy", "po „proponuj” oczekiwano „gdy”");
                    proposeWhen = Expression();
                    EndOfLine();
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "z" })
                {
                    _position++;
                    from = Name().Text;
                    Expect("do", "brak „do”");
                    to = Name().Text;
                    EndOfLine();
                }
                else
                {
                    throw new SyntaxError(Current.Span, "oczekiwano „z”, „proponuj” albo „przejmij”");
                }
            });
            return from is null || to is null
                ? throw new SyntaxError(name.Span, "powiązanie bez „z … do …”")
                : new LinkSyntax(name.Text, from, to, proposeWhen, takeovers, name.Span);
        }

        private TakeoverSyntax Takeover()
        {
            var takeover = Next();
            var features = SeparatedByCommas(Reference);
            Expect("jako", "brak „jako”");
            var @as = Current is { Kind: TokenKind.Name } or { Kind: TokenKind.Keyword, Text: "domyślne" or "zablokowane" }
                ? Next().Text
                : throw new SyntaxError(Current.Span, "po „jako” oczekiwano „domyślne”, „zablokowane” albo nazwy cechy");
            EndOfLine();
            return new TakeoverSyntax(features, @as, takeover.Span);
        }
    }
}
