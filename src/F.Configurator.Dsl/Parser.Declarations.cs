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

        private DeclarationSyntax Declaration()
        {
            _position++;
            var title = Next();
            _position++;
            return new CatalogSyntax(title.Text, Block(Property), title.Span);
        }

        // The lines one level deeper than the line above.
        private List<T> Block<T>(Func<T> line)
        {
            List<T> lines = [];
            if (Current.Kind != TokenKind.Indent)
            {
                return lines;
            }

            _position++;
            while (Current.Kind != TokenKind.Dedent)
            {
                lines.Add(line());
            }

            _position++;
            return lines;
        }

        private PropertySyntax Property()
        {
            var name = Next();
            var value = Value();
            _position++;
            return new PropertySyntax(name.Text, [value], name.Span);
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
