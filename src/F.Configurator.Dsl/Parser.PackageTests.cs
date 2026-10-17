using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static partial class Parser
{
    private sealed partial class Reader
    {
        private TestSyntax Test()
        {
            var title = Text();
            EndOfLine();
            string? collection = null;
            List<AssignmentSyntax> data = [];
            List<ExpectationSyntax> expectations = [];
            Block(() =>
            {
                var word = Current is { Kind: TokenKind.Keyword, Text: "kolekcja" or "dane" or "oczekuj" }
                    ? Next().Text
                    : throw new SyntaxError(Current.Span, "oczekiwano „kolekcja”, „dane” albo „oczekuj”");
                if (word == "kolekcja")
                {
                    collection = Name().Text;
                }
                else if (word == "dane")
                {
                    data.AddRange(SeparatedByCommas(Setting));
                }
                else
                {
                    expectations.AddRange(SeparatedByCommas(Expectation));
                }

                EndOfLine();
            });
            return new TestSyntax(title.Text, collection, data, expectations, title.Span);
        }

        private ExpectationSyntax Expectation()
        {
            if (Current is { Kind: TokenKind.Keyword, Text: "indeks" })
            {
                return IndexExpectation(Next());
            }

            if (Current is { Kind: TokenKind.Keyword, Text: "niedostępne" or "ukryta" or "komunikat" })
            {
                var kind = Next();
                var feature = Reference();
                var options = kind.Text == "niedostępne" ? Expression() : null;
                return new ExpectationSyntax(kind.Text, feature, options, kind.Span);
            }

            if (Current is { Kind: TokenKind.Keyword, Text: "cena" or "składnik" })
            {
                var kind = Next();
                var part = kind.Text == "składnik" ? Reference() : null;
                Expect("=", "brak „=”");
                return new ExpectationSyntax(kind.Text, part, Expression(), kind.Span);
            }

            var target = Reference();
            Expect("=", "brak „=”");
            return new ExpectationSyntax("=", target, Expression(), target.Span);
        }

        private ExpectationSyntax IndexExpectation(Token index)
        {
            var kind = Current switch
            {
                { Kind: TokenKind.Keyword, Text: "zawiera" } => "indeks zawiera",
                { Kind: TokenKind.Operator, Text: "=" } => "indeks",
                _ => throw new SyntaxError(Current.Span, "po „indeks” oczekiwano „zawiera” albo „=”"),
            };
            _position++;
            return new ExpectationSyntax(kind, null, Expression(), index.Span);
        }

        private AssignmentSyntax Setting()
        {
            var target = Reference();
            Expect("=", "brak „=”");
            return new AssignmentSyntax(target, Expression(), target.Span);
        }
    }
}
