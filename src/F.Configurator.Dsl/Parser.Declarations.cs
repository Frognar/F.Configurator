using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static partial class Parser
{
    public static ParseResult<FileSyntax> ParseFile(string file, string source)
    {
        var lexed = Lexer.Tokenize(file, source);
        var reader = new Reader(lexed.Tokens);
        return new ParseResult<FileSyntax>(reader.File(), lexed.Diagnostics);
    }

    private sealed partial class Reader
    {
        public FileSyntax File()
        {
            List<DeclarationSyntax> declarations = [];
            while (Current.Kind != TokenKind.End)
            {
                declarations.Add(Declaration());
            }

            return new FileSyntax(declarations);
        }

        private DeclarationSyntax Declaration() =>
            Next().Text switch
            {
                "cecha" => Feature(),
                _ => Catalog(),
            };

        private CatalogSyntax Catalog()
        {
            var title = Next();
            _position++;
            return new CatalogSyntax(title.Text, Block(Property), title.Span);
        }

        private FeatureSyntax Feature()
        {
            var name = Next();
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
