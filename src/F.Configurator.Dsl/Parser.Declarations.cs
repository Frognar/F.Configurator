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
            return new CatalogSyntax(title.Text, [], title.Span);
        }
    }
}
