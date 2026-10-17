using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static partial class Parser
{
    public static ParseResult<FileSyntax> ParseFile(string file, string source)
    {
        var lexed = Lexer.Tokenize(file, source);
        var reader = new Reader(lexed.Tokens);
        var syntax = reader.File();
        return new ParseResult<FileSyntax>(syntax, [.. lexed.Diagnostics, .. reader.Errors]);
    }

    private sealed partial class Reader
    {
        public List<Diagnostic> Errors { get; } = [];

        public FileSyntax File()
        {
            List<DeclarationSyntax> declarations = [];
            while (Current.Kind != TokenKind.End)
            {
                try
                {
                    declarations.Add(Declaration());
                }
                catch (SyntaxError error)
                {
                    Errors.Add(error.Diagnostic);
                    SkipToNextDeclaration();
                }
            }

            return new FileSyntax(declarations);
        }

        // Every declaration starts a line without indentation. A problem found at the end of a declaration leaves the
        // reader already there.
        private void SkipToNextDeclaration()
        {
            while (Current.Kind != TokenKind.End && !AtDeclaration())
            {
                _position++;
            }
        }

        private bool AtDeclaration() =>
            tokens[_position - 1].Kind is TokenKind.NewLine or TokenKind.Dedent
            && Current.Kind is TokenKind.Name or TokenKind.Keyword
            && Current.Span.Column == 1;

        private DeclarationSyntax Declaration()
        {
            var keyword = Next();
            return keyword.Text switch
            {
                "katalog" => Catalog(),
                "cecha" => Feature(),
                "wyliczana" => Computed(),
                "tabela" => Table(),
                "kontekst" => Context(),
                "kolekcja" => Collection(),
                "reguła" => Rule(),
                "test" => Test(),
                "indeks" => throw new SyntaxError(keyword.Span, "„indeks” może stać tylko w kolekcji"),
                _ => throw new SyntaxError(keyword.Span, "oczekiwano deklaracji"),
            };
        }

        private CatalogSyntax Catalog()
        {
            var title = Text();
            EndOfLine();
            return new CatalogSyntax(title.Text, Block(Property), title.Span);
        }

        private FeatureSyntax Feature()
        {
            var name = Name();
            Expect(":", "brak „:”");
            var type = Current is { Kind: TokenKind.Keyword, Text: "wybór" or "wielokrotny" or "tak/nie" or "liczba" or "tekst" }
                ? Next().Text
                : throw new SyntaxError(
                    Current.Span, "oczekiwano typu cechy (wybór, wielokrotny wybór, tak/nie, liczba, tekst)");
            if (type == "wielokrotny")
            {
                type = $"{type} {Expect("wybór", "po „wielokrotny” oczekiwano „wybór”").Text}";
            }

            EndOfLine();
            List<PropertySyntax> properties = [];
            List<AttributeSyntax> attributes = [];
            List<string> optionFiles = [];
            List<OptionSyntax> options = [];
            Block(() =>
            {
                if (Current is { Kind: TokenKind.Keyword, Text: "atrybut" })
                {
                    attributes.Add(Attribute());
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "opcje" })
                {
                    _position++;
                    Expect("z", "brak „z”");
                    optionFiles.Add(Text().Text);
                    EndOfLine();
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "opcja" })
                {
                    options.Add(Option());
                }
                else
                {
                    properties.Add(Property());
                }
            });
            return new FeatureSyntax(name.Text, type, properties, attributes, optionFiles, options, name.Span);
        }

        private AttributeSyntax Attribute()
        {
            _position++;
            var name = Name();
            Expect(":", "brak „:”");
            var type = Current is { Kind: TokenKind.Keyword, Text: "tekst" or "liczba" or "tak/nie" }
                ? Next()
                : throw new SyntaxError(Current.Span, "oczekiwano typu atrybutu (tekst, liczba, tak/nie)");
            ExpressionSyntax? @default = null;
            if (Current is { Kind: TokenKind.Keyword, Text: "domyślnie" })
            {
                _position++;
                @default = Expression();
            }

            EndOfLine();
            return new AttributeSyntax(name.Text, type.Text, @default, name.Span);
        }

        private OptionSyntax Option()
        {
            _position++;
            var id = Current.Kind switch
            {
                TokenKind.Name or TokenKind.Text => Next(),
                TokenKind.Number => throw new SyntaxError(
                    Current.Span, "identyfikator opcji zaczynający się od cyfry wpisz w cudzysłowie", $"\"{Current.Text}\""),
                _ => throw new SyntaxError(Current.Span, "oczekiwano identyfikatora opcji"),
            };
            var label = Current.Kind == TokenKind.Text ? Next().Text : null;
            EndOfLine();
            return new OptionSyntax(id.Text, label, Block(Property), id.Span);
        }

        private ComputedSyntax Computed()
        {
            var name = Name();
            Expect("=", "brak „=”");
            var expression = Expression();
            EndOfLine();
            return new ComputedSyntax(name.Text, expression, name.Span);
        }

        private TableSyntax Table()
        {
            var name = Name();
            string? file = null;
            if (Current is { Kind: TokenKind.Keyword, Text: "z" })
            {
                _position++;
                file = Text().Text;
            }

            EndOfLine();
            List<string> keys = [];
            List<string> values = [];
            string? allowed = null;
            List<TableRowSyntax> rows = [];
            Block(() =>
            {
                if (Current is { Kind: TokenKind.Operator, Text: "|" })
                {
                    rows.Add(Row());
                    return;
                }

                var word = Next().Text;
                if (word == "dozwolone")
                {
                    allowed = Name().Text;
                }
                else
                {
                    (word == "klucz" ? keys : values).AddRange(Names());
                }

                EndOfLine();
            });
            return new TableSyntax(name.Text, file, keys, values, allowed, rows, name.Span);
        }

        // `| PL | "80" | 844 |`
        private TableRowSyntax Row()
        {
            var start = Next();
            List<IReadOnlyList<ExpressionSyntax>> cells = [];
            while (Current.Kind != TokenKind.NewLine)
            {
                cells.Add(Cell());
                Expect("|", "brak „|” na końcu wiersza tabeli");
            }

            EndOfLine();
            return new TableRowSyntax(cells, start.Span);
        }

        // An empty cell, or values separated by commas.
        private List<ExpressionSyntax> Cell() =>
            Current is { Kind: TokenKind.Operator, Text: "|" } ? [] : SeparatedByCommas(Expression);

        private ContextSyntax Context()
        {
            var name = Name();
            EndOfLine();
            List<AttributeSyntax> attributes = [];
            string? valuesFile = null;
            Block(() =>
            {
                if (Current is { Kind: TokenKind.Keyword, Text: "atrybut" })
                {
                    attributes.Add(Attribute());
                    return;
                }

                _position++;
                Expect("z", "brak „z”");
                valuesFile = Text().Text;
                EndOfLine();
            });
            return new ContextSyntax(name.Text, attributes, valuesFile, name.Span);
        }

        // The lines one level deeper than the line above.
        private List<T> Block<T>(Func<T> line)
        {
            List<T> lines = [];
            Block(() => lines.Add(line()));
            return lines;
        }

        private void Block(Action line)
        {
            if (Current.Kind != TokenKind.Indent)
            {
                return;
            }

            _position++;
            while (Current.Kind != TokenKind.Dedent)
            {
                line();
            }

            _position++;
        }

        private PropertySyntax Property()
        {
            var name = Next();
            if (Current.Kind == TokenKind.NewLine)
            {
                throw new SyntaxError(Current.Span, $"po „{name.Text}” oczekiwano wartości");
            }

            var values = SeparatedByCommas(Value);
            EndOfLine();
            return new PropertySyntax(name.Text, values, name.Span);
        }

        // `reakcja błąd`: a keyword alone is a word, not the start of an expression.
        private ExpressionSyntax Value()
        {
            if (Current.Kind == TokenKind.Keyword && tokens[_position + 1].Kind == TokenKind.NewLine)
            {
                var word = Next();
                return new NameSyntax(word.Text, word.Span);
            }

            return Expression();
        }

        private void EndOfLine()
        {
            if (Current.Kind != TokenKind.NewLine)
            {
                throw new SyntaxError(Current.Span, $"nieoczekiwane „{Current.Text}”");
            }

            _position++;
        }

        private Token Text() =>
            Current.Kind == TokenKind.Text
                ? Next()
                : throw new SyntaxError(Current.Span, "oczekiwano tekstu w cudzysłowie");

        private Token Name() =>
            Current.Kind switch
            {
                TokenKind.Name => Next(),
                TokenKind.Keyword => throw new SyntaxError(
                    Current.Span, $"„{Current.Text}” to słowo kluczowe i nie może być nazwą"),
                _ => throw new SyntaxError(Current.Span, "oczekiwano nazwy"),
            };

        private ExpressionSyntax Reference()
        {
            var name = Name();
            ExpressionSyntax reference = new NameSyntax(name.Text, name.Span);
            while (Current is { Kind: TokenKind.Operator, Text: "." })
            {
                _position++;
                var member = Name();
                reference = new MemberSyntax(reference, member.Text, Cover(reference.Span, member.Span));
            }

            return reference;
        }

        private List<string> Names() => SeparatedByCommas(() => Name().Text);
    }
}
