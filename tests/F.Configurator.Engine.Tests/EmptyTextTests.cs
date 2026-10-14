using static F.Configurator.Engine.Tests.IndexPatterns;

namespace F.Configurator.Engine.Tests;

// Decision 2026-10-14: an empty text is no value. A user who clears a text field leaves the feature empty, as if
// it had never been filled in, so `wybrana(…)`, the stages, the index and the lengths see it as missing.
// Frognar 2026-10-14: the same holds for every value a feature gets (default, forced, computed). Inside an expression
// "" stays a text, so `jeśli wybrana(X) to … inaczej ""` in an index pattern still joins.
public class EmptyTextTests
{
    private static readonly Expression Pattern = Add(
        SymbolOf("Model", ("PORTA", "P"), ("VERTE", "V")),
        If(HasValue(Reference("Grawer")), Add(Text("G"), Reference("Grawer")), Text("")));

    private static Configuration Start(
        Func<CatalogBuilder, CatalogBuilder>? addRules = null,
        bool engravingRequired = false)
    {
        var catalog = (addRules ?? (c => c))(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Text("Grawer", feature => engravingRequired ? feature : feature.Optional())
                .Collection("Basic", collection => collection
                    .Stage("Model", "Model")
                    .Stage("Wykonczenie", "Grawer")
                    .Index(Pattern)))
            .Build();

        return Configuration.Start(catalog, "Basic").Set("Model", Value.Option("PORTA"));
    }

    [Fact]
    public void Empty_text_leaves_the_feature_without_a_value()
    {
        var configuration = Start().Set("Grawer", Value.Text(""));

        Assert.Equal(Value.Missing, configuration.ValueOf("Grawer"));
        Assert.Null(configuration.SourceOf("Grawer"));
    }

    [Fact]
    public void Empty_text_clears_the_text_entered_before()
    {
        var configuration = Start()
            .Set("Grawer", Value.Text("ABC"))
            .Set("Grawer", Value.Text(""));

        Assert.Equal(Value.Missing, configuration.ValueOf("Grawer"));
    }

    // The user cleared the text; no rule changed what they entered.
    [Fact]
    public void Clearing_a_text_lists_no_change()
    {
        var configuration = Start()
            .Set("Grawer", Value.Text("ABC"))
            .Set("Grawer", Value.Text(""));

        Assert.Empty(configuration.Changes);
    }

    [Fact]
    public void Empty_text_does_not_fill_in_a_required_feature()
    {
        var configuration = Start(engravingRequired: true).Set("Grawer", Value.Text(""));

        Assert.False(configuration.CanFinish);
        Assert.Null(configuration.Index);
    }

    [Fact]
    public void Index_leaves_out_an_optional_text_that_was_cleared()
    {
        var configuration = Start()
            .Set("Grawer", Value.Text("ABC"))
            .Set("Grawer", Value.Text(""));

        Assert.Equal("P", configuration.Index);
    }

    // A length is checked on a text, and an empty one is no text; a required feature catches it instead.
    [Fact]
    public void Empty_text_is_not_checked_against_the_length()
    {
        var configuration = Start(catalog => catalog.Rule("GrawerKrotki", rule => rule
                .Length("Grawer", Expression.Range(2m, 5m))
                .OnViolation(Reaction.Error, "Grawer ma od 2 do 5 znaków.")))
            .Set("Grawer", Value.Text(""));

        Assert.Empty(configuration.Violations);
        Assert.True(configuration.CanFinish);
    }

    // Like a value the user never set: once the text is cleared, the default fills the feature again.
    [Fact]
    public void Default_comes_back_after_the_text_is_cleared()
    {
        var configuration = Start(catalog => catalog.Rule("GrawerDomyslny", rule => rule
                .Default("Grawer", Text("KOWALSCY"))))
            .Set("Grawer", Value.Text("ABC"))
            .Set("Grawer", Value.Text(""));

        Assert.Equal(Value.Text("KOWALSCY"), configuration.ValueOf("Grawer"));
        Assert.Equal(ValueSource.Default, configuration.SourceOf("Grawer"));
    }

    [Fact]
    public void Empty_default_leaves_the_feature_without_a_value()
    {
        var configuration = Start(catalog => catalog.Rule("GrawerDomyslny", rule => rule
            .Default("Grawer", Text(""))));

        Assert.Equal(Value.Missing, configuration.ValueOf("Grawer"));
        Assert.Null(configuration.SourceOf("Grawer"));
    }

    [Fact]
    public void Empty_default_does_not_fill_in_a_required_feature()
    {
        var configuration = Start(
            catalog => catalog.Rule("GrawerDomyslny", rule => rule.Default("Grawer", Text(""))),
            engravingRequired: true);

        Assert.False(configuration.CanFinish);
    }

    [Fact]
    public void Empty_forced_text_leaves_the_feature_without_a_value()
    {
        var configuration = Start(catalog => catalog.Rule("BezGrawerowania", rule => rule
                .When(Equal(Reference("Model"), Option("VERTE")))
                .Set("Grawer", Text(""))))
            .Set("Grawer", Value.Text("ABC"))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Grawer"));
    }

    [Fact]
    public void Computed_feature_that_gives_an_empty_text_has_no_value()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
            .Computed("Seria", If(Equal(Reference("Model"), Option("VERTE")), Text("V"), Text("")))
            .Collection("Basic", collection => collection.Stage("Model", "Model"))
            .Build();

        var configuration = Configuration.Start(catalog, "Basic").Set("Model", Value.Option("PORTA"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Seria"));
    }
}
