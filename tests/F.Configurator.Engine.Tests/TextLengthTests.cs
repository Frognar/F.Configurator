namespace F.Configurator.Engine.Tests;

// Grammar 4.2 and 4.7: `długość 0..20` on a text feature gives a fixed length; `długość Grawer = 0..12` in a rule
// gives a dependent one. Lengths combine like number limits: the highest priority wins, a rule replaces the
// length declared on the feature, and on a tie the most restrictive bound wins. A text of the wrong length reacts
// like a number outside its limits.
public class TextLengthTests
{
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(
        Func<CatalogBuilder, CatalogBuilder> addRules,
        Func<TextFeatureBuilder, TextFeatureBuilder>? engraving = null)
    {
        var catalog = addRules(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Text("Grawer", feature => (engraving ?? (e => e))(feature))
                .Collection("Basic", collection => collection
                    .Stage("Model", "Model")
                    .Stage("Wykonczenie", "Grawer")))
            .Build();

        return Configuration.Start(catalog, "Basic").Set("Model", Value.Option("PORTA"));
    }

    private static Func<CatalogBuilder, CatalogBuilder> ShortEngraving(Reaction reaction) =>
        catalog => catalog.Rule("GrawerKrotki", rule => rule
            .Length("Grawer", Expression.Range(2m, 5m))
            .OnViolation(reaction, "Grawer ma od 2 do 5 znaków."));

    [Fact]
    public void Text_feature_takes_text()
    {
        var configuration = Start(catalog => catalog).Set("Grawer", Value.Text("ABC"));

        Assert.Equal(Value.Text("ABC"), configuration.ValueOf("Grawer"));
    }

    [Fact]
    public void Text_without_length_has_none()
    {
        var configuration = Start(catalog => catalog);

        Assert.Equal(new TextLength(null, null), configuration.LengthOf("Grawer"));
    }

    [Fact]
    public void Length_declared_on_the_feature_applies()
    {
        var configuration = Start(catalog => catalog, engraving => engraving.Length(0, 20));

        Assert.Equal(new TextLength(0, 20), configuration.LengthOf("Grawer"));
    }

    // Like number limits: the rule replaces the declared length, even a looser one.
    [Fact]
    public void Rule_replaces_the_length_declared_on_the_feature()
    {
        var configuration = Start(
            catalog => catalog.Rule("GrawerDlugi", rule => rule.Length("Grawer", Expression.Range(0m, 30m))),
            engraving => engraving.Length(0, 20));

        Assert.Equal(new TextLength(0, 30), configuration.LengthOf("Grawer"));
    }

    [Fact]
    public void Length_applies_only_while_its_condition_holds()
    {
        var start = Start(catalog => catalog
            .Rule("GrawerVerte", rule => rule.When(ModelVerte).Length("Grawer", Expression.Range(0m, 12m))));

        Assert.Equal(new TextLength(0, 12), start.Set("Model", Value.Option("VERTE")).LengthOf("Grawer"));
        Assert.Equal(new TextLength(null, null), start.LengthOf("Grawer"));
    }

    [Fact]
    public void Higher_priority_length_wins()
    {
        var configuration = Start(catalog => catalog
            .Rule("GrawerKrotki", rule => rule.Length("Grawer", Expression.Range(0m, 12m)))
            .Rule("GrawerDlugi", rule => rule.Priority(10).Length("Grawer", Expression.Range(0m, 30m))));

        Assert.Equal(new TextLength(0, 30), configuration.LengthOf("Grawer"));
    }

    // On a tie each bound is the most restrictive one: the largest minimum and the smallest maximum.
    [Fact]
    public void Tie_of_lengths_takes_the_most_restrictive_bounds()
    {
        var configuration = Start(catalog => catalog
            .Rule("GrawerMin", rule => rule.Length("Grawer", Expression.Range(2m, 20m)))
            .Rule("GrawerMaks", rule => rule.Length("Grawer", Expression.Range(0m, 12m))));

        Assert.Equal(new TextLength(2, 12), configuration.LengthOf("Grawer"));
    }

    [Fact]
    public void Length_of_a_feature_that_is_not_text_is_a_programming_error()
    {
        var configuration = Start(catalog => catalog);

        Assert.Throws<ArgumentException>(() => configuration.LengthOf("Model"));
    }

    [Fact]
    public void Text_within_the_length_violates_nothing()
    {
        var configuration = Start(ShortEngraving(Reaction.Error)).Set("Grawer", Value.Text("ABC"));

        Assert.Empty(configuration.Violations);
        Assert.True(configuration.CanFinish);
    }

    // The nearest allowed text keeps the first characters.
    [Fact]
    public void Correct_cuts_text_to_the_maximum_length()
    {
        var configuration = Start(ShortEngraving(Reaction.Correct)).Set("Grawer", Value.Text("ABCDEFG"));

        Assert.Equal(Value.Text("ABCDE"), configuration.ValueOf("Grawer"));
        Assert.Equal(
            [new RuleViolation("Grawer", "GrawerKrotki", Reaction.Correct, "Grawer ma od 2 do 5 znaków.")],
            configuration.Violations);
    }

    // A too short text has no nearest allowed text, so `koryguj` clears it, like an excluded option.
    [Fact]
    public void Correct_clears_text_that_is_too_short()
    {
        var configuration = Start(ShortEngraving(Reaction.Correct)).Set("Grawer", Value.Text("A"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Grawer"));
        Assert.Equal(
            [new RuleViolation("Grawer", "GrawerKrotki", Reaction.Correct, "Grawer ma od 2 do 5 znaków.")],
            configuration.Violations);
    }

    [Fact]
    public void Error_keeps_text_of_the_wrong_length_and_blocks_finishing()
    {
        var configuration = Start(ShortEngraving(Reaction.Error)).Set("Grawer", Value.Text("ABCDEFG"));

        Assert.Equal(Value.Text("ABCDEFG"), configuration.ValueOf("Grawer"));
        Assert.Equal(
            [new RuleViolation("Grawer", "GrawerKrotki", Reaction.Error, "Grawer ma od 2 do 5 znaków.")],
            configuration.Violations);
        Assert.False(configuration.CanFinish);
    }

    [Fact]
    public void Length_declared_on_the_feature_reacts_with_the_catalog_reaction()
    {
        var configuration = Start(catalog => catalog.DefaultReaction(Reaction.Warning), engraving => engraving.Length(0, 5))
            .Set("Grawer", Value.Text("ABCDEFG"));

        Assert.Equal([new RuleViolation("Grawer", null, Reaction.Warning, null)], configuration.Violations);
        Assert.True(configuration.CanFinish);
    }
}
