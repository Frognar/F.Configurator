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
            return new CollectionSyntax(name.Text, properties, stages, features, components, shared, own, null, [], name.Span);
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
                _position += 2;
                visibleWhen = Expression();
            }

            return new CollectionFeatureSyntax(name.Text, visibleWhen, name.Span);
        }
    }
}
