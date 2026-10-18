using System.Globalization;
using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static partial class Parser
{
    private sealed partial class Reader
    {
        private PriceListSyntax PriceList()
        {
            var name = Name();
            string? extends = null;
            if (Current is { Kind: TokenKind.Keyword, Text: "rozszerza" })
            {
                _position++;
                extends = Name().Text;
            }

            var isDefault = Current is { Kind: TokenKind.Keyword, Text: "domyślny" };
            if (isDefault)
            {
                _position++;
            }

            EndOfLine();
            List<PropertySyntax> properties = [];
            BaseSyntax? @base = null;
            List<string> optionFiles = [];
            List<SurchargeSyntax> surcharges = [];
            List<string> disabled = [];
            List<OverrideSyntax> overrides = [];
            List<AdjustmentSyntax> adjustments = [];
            List<DeclarationSyntax> declarations = [];
            Block(() =>
            {
                if (Current is { Kind: TokenKind.Keyword, Text: "baza" })
                {
                    @base = Base();
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "dopłata" })
                {
                    surcharges.Add(Surcharge());
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "wyłącz" })
                {
                    _position++;
                    disabled.AddRange(Names());
                    EndOfLine();
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "nadpisz" })
                {
                    overrides.Add(Override());
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "rabat" or "narzut" or "zaokrąglij" })
                {
                    adjustments.Add(Adjustment());
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "tabela" })
                {
                    declarations.Add(Declaration());
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "opcje" })
                {
                    _position++;
                    Expect("z", "brak „z”");
                    optionFiles.Add(Text().Text);
                    EndOfLine();
                }
                else
                {
                    properties.Add(Property());
                }
            });
            return new PriceListSyntax(
                name.Text,
                extends,
                isDefault,
                properties,
                @base,
                optionFiles,
                surcharges,
                disabled,
                overrides,
                adjustments,
                declarations,
                name.Span);
        }

        private SurchargeSyntax Surcharge()
        {
            _position++;
            var id = Name();
            var label = Text();
            EndOfLine();
            ExpressionSyntax? condition = null;
            ExpressionSyntax? amount = null;
            Block(() =>
            {
                var word = Current is { Kind: TokenKind.Keyword, Text: "gdy" or "kwota" }
                    ? Next().Text
                    : throw new SyntaxError(Current.Span, "oczekiwano „gdy” albo „kwota”");
                if (word == "gdy")
                {
                    condition = Expression();
                }
                else
                {
                    amount = Expression();
                }

                EndOfLine();
            });
            return amount is null
                ? throw new SyntaxError(id.Span, "dopłata bez „kwota”")
                : new SurchargeSyntax(id.Text, label.Text, condition, amount, id.Span);
        }

        private OverrideSyntax Override()
        {
            _position++;
            var id = Name();
            Expect("kwota", "brak „kwota”");
            var amount = Expression();
            EndOfLine();
            return new OverrideSyntax(id.Text, amount, id.Span);
        }

        private AdjustmentSyntax Adjustment()
        {
            var kind = Next();
            var percent = kind.Text != "zaokrąglij";
            var message = percent ? $"po „{kind.Text}” oczekiwano procentu, np. 5%" : "po „zaokrąglij” oczekiwano liczby";
            var value = Current.Kind == TokenKind.Number
                ? decimal.Parse(Next().Text, CultureInfo.InvariantCulture)
                : throw new SyntaxError(Current.Span, message);
            if (percent)
            {
                Expect("%", message);
            }

            EndOfLine();
            return new AdjustmentSyntax(kind.Text, value, kind.Span);
        }

        private BaseSyntax Base()
        {
            var @base = Next();
            string? file = null;
            List<ExpressionSyntax> keys = [];
            if (Current is { Kind: TokenKind.Keyword, Text: "z" })
            {
                _position++;
                file = Text().Text;
                if (Current is { Kind: TokenKind.Keyword, Text: "klucz" })
                {
                    _position++;
                    keys.AddRange(SeparatedByCommas(Reference));
                }
            }

            EndOfLine();
            List<TableRowSyntax> rows = [];
            Block(() =>
            {
                if (Current is { Kind: TokenKind.Operator, Text: "|" })
                {
                    rows.Add(Row());
                    return;
                }

                Expect("klucz", "oczekiwano „klucz” albo wiersza tabeli");
                keys.AddRange(SeparatedByCommas(Reference));
                EndOfLine();
            });
            return new BaseSyntax(file, keys, rows, @base.Span);
        }

        private PriceListAssignmentSyntax PriceListAssignment()
        {
            var name = Name();
            Expect("do", "brak „do”");
            List<string> Customers() => SeparatedByCommas(() => Text().Text);
            List<string> customers = [];
            string? customersFile = null;
            var kind = Current is { Kind: TokenKind.Keyword, Text: "kontrahenta" or "kontrahentów" }
                ? Next().Text
                : throw new SyntaxError(Current.Span, "oczekiwano „kontrahenta” albo „kontrahentów”");
            if (kind == "kontrahenta")
            {
                customers.Add(Text().Text);
            }
            else if (Current is { Kind: TokenKind.Keyword, Text: "z" })
            {
                _position++;
                customersFile = Text().Text;
            }
            else if (Current is { Kind: TokenKind.Operator, Text: "[" })
            {
                _position++;
                customers.AddRange(Customers());
                Expect("]", "brak „]”");
            }
            else if (Current.Kind == TokenKind.Text)
            {
                customers.AddRange(Customers());
            }
            else
            {
                throw new SyntaxError(
                    Current.Span, "po „kontrahentów” oczekiwano kontrahentów w cudzysłowie albo „z” i nazwy pliku");
            }

            EndOfLine();
            return new PriceListAssignmentSyntax(name.Text, customers, customersFile, name.Span);
        }
    }
}
