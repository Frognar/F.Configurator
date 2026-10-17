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
            try
            {
                while (Current.Kind != TokenKind.End)
                {
                    declarations.Add(Declaration());
                }
            }
            catch (SyntaxError error)
            {
                Errors.Add(error.Diagnostic);
            }

            return new FileSyntax(declarations);
        }

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
                _ => throw new SyntaxError(keyword.Span, "oczekiwano deklaracji"),
            };
        }

        private CatalogSyntax Catalog()
        {
            var title = Next();
            _position++;
            return new CatalogSyntax(title.Text, Block(Property), title.Span);
        }

        private FeatureSyntax Feature()
        {
            var name = Name();
            _position++;
            var type = Next().Text;
            if (type == "wielokrotny")
            {
                type = $"{type} {Next().Text}";
            }

            _position++;
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
                    _position += 2;
                    optionFiles.Add(Next().Text);
                    _position++;
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

        private OptionSyntax Option()
        {
            _position++;
            var id = Next();
            var label = Current.Kind == TokenKind.Text ? Next().Text : null;
            _position++;
            return new OptionSyntax(id.Text, label, Block(Property), id.Span);
        }

        private ComputedSyntax Computed()
        {
            var name = Name();
            _position++;
            var expression = Expression();
            _position++;
            return new ComputedSyntax(name.Text, expression, name.Span);
        }

        private TableSyntax Table()
        {
            var name = Name();
            string? file = null;
            if (Current is { Kind: TokenKind.Keyword, Text: "z" })
            {
                _position++;
                file = Next().Text;
            }

            _position++;
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
                    allowed = Next().Text;
                }
                else
                {
                    (word == "klucz" ? keys : values).AddRange(Names());
                }

                _position++;
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
                _position++;
            }

            _position++;
            return new TableRowSyntax(cells, start.Span);
        }

        // An empty cell, or values separated by commas.
        private List<ExpressionSyntax> Cell()
        {
            List<ExpressionSyntax> values = [];
            if (Current is { Kind: TokenKind.Operator, Text: "|" })
            {
                return values;
            }

            values.Add(Expression());
            while (Current is { Kind: TokenKind.Operator, Text: "," })
            {
                _position++;
                values.Add(Expression());
            }

            return values;
        }

        private Token Name() =>
            Current.Kind switch
            {
                TokenKind.Name => Next(),
                TokenKind.Keyword => throw new SyntaxError(
                    Current.Span, $"„{Current.Text}” to słowo kluczowe i nie może być nazwą"),
                _ => throw new SyntaxError(Current.Span, "oczekiwano nazwy"),
            };

        // Names separated by commas.
        private List<string> Names()
        {
            List<string> names = [Next().Text];
            while (Current is { Kind: TokenKind.Operator, Text: "," })
            {
                _position++;
                names.Add(Next().Text);
            }

            return names;
        }

        private ContextSyntax Context()
        {
            var name = Name();
            _position++;
            List<AttributeSyntax> attributes = [];
            string? valuesFile = null;
            Block(() =>
            {
                if (Current is { Kind: TokenKind.Keyword, Text: "atrybut" })
                {
                    attributes.Add(Attribute());
                    return;
                }

                _position += 2;
                valuesFile = Next().Text;
                _position++;
            });
            return new ContextSyntax(name.Text, attributes, valuesFile, name.Span);
        }

        private AttributeSyntax Attribute()
        {
            _position++;
            var name = Next();
            _position++;
            var type = Next();
            ExpressionSyntax? @default = null;
            if (Current is { Kind: TokenKind.Keyword, Text: "domyślnie" })
            {
                _position++;
                @default = Expression();
            }

            _position++;
            return new AttributeSyntax(name.Text, type.Text, @default, name.Span);
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
            List<ExpressionSyntax> values = [Value()];
            while (Current is { Kind: TokenKind.Operator, Text: "," })
            {
                _position++;
                values.Add(Value());
            }

            _position++;
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
    }
}
