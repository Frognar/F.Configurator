using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static partial class Parser
{
    private sealed partial class Reader
    {
        private CollectionSyntax Collection()
        {
            var name = Name();
            EndOfLine();
            List<PropertySyntax> properties = [];
            List<StageSyntax> stages = [];
            List<CollectionFeatureSyntax> features = [];
            List<ComponentSyntax> components = [];
            List<string> shared = [];
            List<string> own = [];
            List<DeclarationSyntax> declarations = [];
            ExpressionSyntax? index = null;
            List<LayerSyntax>? visualization = null;
            Block(() =>
            {
                if (Current is { Kind: TokenKind.Keyword, Text: "etap" })
                {
                    stages.Add(Stage());
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "cechy" })
                {
                    features.AddRange(Features());
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "tabela" or "wyliczana" or "reguła" })
                {
                    declarations.Add(Declaration());
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "indeks" })
                {
                    index = index is null ? Index() : throw new SyntaxError(Current.Span, "kolekcja ma już indeks");
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "wizualizacja" })
                {
                    visualization = visualization is null
                        ? Visualization()
                        : throw new SyntaxError(Current.Span, "kolekcja ma już wizualizację");
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "komponent" })
                {
                    components.Add(Component());
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "wspólne" or "własne" })
                {
                    (Next().Text == "wspólne" ? shared : own).AddRange(Names());
                    EndOfLine();
                }
                else
                {
                    properties.Add(Property());
                }
            });
            return new CollectionSyntax(name.Text, properties, stages, features, components, shared, own, index, visualization, declarations, name.Span);
        }

        // `indeks` and its expression in the deeper lines below.
        private ExpressionSyntax Index()
        {
            _position++;
            EndOfLine();
            if (Current.Kind != TokenKind.Indent)
            {
                throw new SyntaxError(Current.Span, "pod „indeks” oczekiwano wyrażenia we wciętych wierszach");
            }

            _position++;
            var index = Expression();
            EndOfLine();
            if (Current.Kind != TokenKind.Dedent)
            {
                throw new SyntaxError(Current.Span, "indeks to jedno wyrażenie, dalszy ciąg zacznij od „+”");
            }

            _position++;
            return index;
        }

        private List<LayerSyntax> Visualization()
        {
            _position++;
            EndOfLine();
            return Block(Layer);
        }

        private LayerSyntax Layer()
        {
            Expect("warstwa", "oczekiwano „warstwa”");
            var image = Text();
            ExpressionSyntax? when = null;
            if (Current is { Kind: TokenKind.Keyword, Text: "gdy" })
            {
                _position++;
                when = Expression();
            }

            EndOfLine();
            ExpressionSyntax? texture = null;
            ExpressionSyntax? mirrorWhen = null;
            Block(() =>
            {
                if (Current is { Kind: TokenKind.Keyword, Text: "tekstura" })
                {
                    _position++;
                    texture = Reference();
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "odbij" })
                {
                    _position++;
                    Expect("gdy", "po „odbij” oczekiwano „gdy”");
                    mirrorWhen = Expression();
                }
                else
                {
                    throw new SyntaxError(Current.Span, "oczekiwano „tekstura” albo „odbij gdy”");
                }

                EndOfLine();
            });
            return new LayerSyntax(image.Text, when, texture, mirrorWhen, image.Span);
        }

        private ComponentSyntax Component()
        {
            _position++;
            var name = Name();
            Expect(":", "brak „:”");
            var collection = Name();
            EndOfLine();
            return new ComponentSyntax(name.Text, collection.Text, name.Span);
        }

        private StageSyntax Stage()
        {
            _position++;
            var title = Text();
            EndOfLine();
            List<CollectionFeatureSyntax> features = [];
            Block(() => features.AddRange(Features()));
            return new StageSyntax(title.Text, features, title.Span);
        }

        private List<CollectionFeatureSyntax> Features()
        {
            _position++;
            var features = SeparatedByCommas(CollectionFeature);
            EndOfLine();
            return features;
        }

        private CollectionFeatureSyntax CollectionFeature()
        {
            var name = Name();
            ExpressionSyntax? visibleWhen = null;
            if (Current is { Kind: TokenKind.Keyword, Text: "widoczna" })
            {
                _position++;
                Expect("gdy", "po „widoczna” oczekiwano „gdy”");
                visibleWhen = Expression();
            }

            return new CollectionFeatureSyntax(name.Text, visibleWhen, name.Span);
        }
    }
}
