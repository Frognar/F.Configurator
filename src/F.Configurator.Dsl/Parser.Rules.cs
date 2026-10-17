using F.Configurator.Dsl.Syntax;

namespace F.Configurator.Dsl;

public static partial class Parser
{
    private sealed partial class Reader
    {
        private RuleSyntax Rule()
        {
            var name = Name();
            EndOfLine();
            List<PropertySyntax> properties = [];
            ExpressionSyntax? condition = null;
            List<EffectSyntax> effects = [];
            ReactionSyntax? reaction = null;
            Block(() =>
            {
                if (Current is { Kind: TokenKind.Keyword, Text: "gdy" })
                {
                    _position++;
                    condition = Expression();
                    EndOfLine();
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "wtedy" })
                {
                    _position++;
                    effects.Add(Effect());
                    effects.AddRange(Block(Effect));
                }
                else if (Current is { Kind: TokenKind.Keyword, Text: "przy" })
                {
                    reaction = Reaction();
                }
                else
                {
                    properties.Add(Property());
                }
            });
            return effects.Count == 0
                ? throw new SyntaxError(name.Span, "reguła bez „wtedy”")
                : new RuleSyntax(name.Text, properties, condition, effects, reaction, name.Span);
        }

        private ReactionSyntax Reaction()
        {
            _position++;
            Expect("naruszeniu", "po „przy” oczekiwano „naruszeniu”");
            var kind = Current is { Kind: TokenKind.Keyword, Text: "koryguj" or "błąd" or "ostrzeż" }
                ? Next()
                : throw new SyntaxError(Current.Span, "oczekiwano reakcji (koryguj, błąd, ostrzeż)");
            var message = Text();
            EndOfLine();
            return new ReactionSyntax(kind.Text, message.Text, kind.Span);
        }

        private EffectSyntax Effect()
        {
            var kind = Next();
            var effect = kind.Text switch
            {
                "tylko" or "zabroń" => Options(kind),
                "min" or "maks" or "krok" or "długość" or "znaki" => Limit(kind),
                "domyślnie" or "ustaw" => Assignment(kind),
                "błąd" or "ostrzeż" or "informuj" => Message(kind),
                "ukryj" or "zablokuj" => new EffectSyntax(kind.Text, SeparatedByCommas(Reference), null, null, kind.Span),
                _ => throw new SyntaxError(
                    kind.Span,
                    "oczekiwano skutku (ukryj, zablokuj, tylko, zabroń, min, maks, krok, długość, znaki, ustaw, domyślnie, "
                    + "błąd, ostrzeż, informuj)"),
            };
            EndOfLine();
            return effect;
        }

        private EffectSyntax Limit(Token kind)
        {
            var target = Reference();
            Expect("=", "brak „=”");
            return new EffectSyntax(kind.Text, [target], Expression(), null, kind.Span);
        }

        private EffectSyntax Assignment(Token kind)
        {
            var targets = SeparatedByCommas(Reference);
            if (Current is { Kind: TokenKind.Keyword, Text: "z" })
            {
                _position++;
                return new EffectSyntax(kind.Text, targets, null, Name().Text, kind.Span);
            }

            Expect("=", "brak „=”");
            return new EffectSyntax(kind.Text, targets, Expression(), null, kind.Span);
        }

        private EffectSyntax Message(Token kind)
        {
            var text = Text();
            return new EffectSyntax(kind.Text, [], new TextSyntax(text.Text, text.Span), null, kind.Span);
        }

        private EffectSyntax Options(Token kind)
        {
            var target = Reference();
            if (Current is { Kind: TokenKind.Keyword, Text: "z" })
            {
                _position++;
                return new EffectSyntax(kind.Text, [target], null, Name().Text, kind.Span);
            }

            return Current is { Kind: TokenKind.Operator, Text: "[" }
                ? new EffectSyntax(kind.Text, [target], Expression(), null, kind.Span)
                : throw new SyntaxError(Current.Span, "oczekiwano listy opcji w nawiasach [ ] albo „z” i nazwy tabeli");
        }
    }
}
