namespace F.Configurator.Engine.Tests;

// Analysis 2.5: a chosen option that a rule later excludes (`tylko`, `zabroń`) triggers that rule's reaction,
// like a number outside its limits (decided 2026-10-12). `koryguj` has no nearest option, so it clears the
// choice; a default may then fill it again. `błąd` and `ostrzeż` keep the choice and report it.
public class UnavailableChoiceTests
{
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(Func<CatalogBuilder, CatalogBuilder> addRules)
    {
        var catalog = addRules(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Choice("Norma", feature => feature.Option("PL", "Polska").Option("CZ", "Czeska").Option("DE", "Niemiecka"))
                .Collection("Basic", collection => collection.Stage("Drzwi", "Model", "Norma")))
            .Build();

        return Configuration.Start(catalog, "Basic")
            .Set("Model", Value.Option("PORTA"))
            .Set("Norma", Value.Option("CZ"));
    }

    private static Func<CatalogBuilder, CatalogBuilder> VerteWithoutCz(Reaction reaction) =>
        catalog => catalog.Rule("VerteBezCZ", rule => rule
            .When(ModelVerte)
            .Forbid("Norma", "CZ")
            .OnViolation(reaction, "Verte nie ma normy czeskiej."));

    [Fact]
    public void Correct_clears_the_choice()
    {
        var configuration = Start(VerteWithoutCz(Reaction.Correct)).Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Norma"));
        Assert.Equal(
            [new RuleViolation("Norma", "VerteBezCZ", Reaction.Correct, "Verte nie ma normy czeskiej.")],
            configuration.Violations);
    }

    [Fact]
    public void Cleared_choice_takes_the_default()
    {
        var configuration = Start(catalog => VerteWithoutCz(Reaction.Correct)(catalog)
                .Rule("NormaPL", rule => rule.Default("Norma", Option("PL"))))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Option("PL"), configuration.ValueOf("Norma"));
        Assert.Equal(ValueSource.Default, configuration.SourceOf("Norma"));
    }

    [Fact]
    public void Error_keeps_the_choice_and_blocks_finishing()
    {
        var configuration = Start(VerteWithoutCz(Reaction.Error)).Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Option("CZ"), configuration.ValueOf("Norma"));
        Assert.Equal(
            [new RuleViolation("Norma", "VerteBezCZ", Reaction.Error, "Verte nie ma normy czeskiej.")],
            configuration.Violations);
        Assert.False(configuration.CanFinish);
    }

    [Fact]
    public void Error_goes_when_the_rule_stops_matching()
    {
        var configuration = Start(VerteWithoutCz(Reaction.Error))
            .Set("Model", Value.Option("VERTE"))
            .Set("Model", Value.Option("PORTA"));

        Assert.Empty(configuration.Violations);
        Assert.True(configuration.CanFinish);
    }

    [Fact]
    public void Warning_keeps_the_choice_and_allows_finishing()
    {
        var configuration = Start(VerteWithoutCz(Reaction.Warning)).Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Option("CZ"), configuration.ValueOf("Norma"));
        Assert.Equal(
            [new RuleViolation("Norma", "VerteBezCZ", Reaction.Warning, "Verte nie ma normy czeskiej.")],
            configuration.Violations);
        Assert.True(configuration.CanFinish);
    }

    [Fact]
    public void Choice_left_out_by_only_reacts_like_a_forbidden_one()
    {
        var configuration = Start(catalog => catalog
                .Rule("VerteTylkoPL", rule => rule.When(ModelVerte).Only("Norma", "PL")))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal([new RuleViolation("Norma", "VerteTylkoPL", Reaction.Error, null)], configuration.Violations);
    }

    // Grammar 4.1: a rule without `przy naruszeniu` uses the catalog's `reakcja`.
    [Fact]
    public void Rule_without_its_own_reaction_uses_the_catalog_reaction()
    {
        var configuration = Start(catalog => catalog
                .DefaultReaction(Reaction.Correct)
                .Rule("VerteBezCZ", rule => rule.When(ModelVerte).Forbid("Norma", "CZ")))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Norma"));
        Assert.Equal([new RuleViolation("Norma", "VerteBezCZ", Reaction.Correct, null)], configuration.Violations);
    }
}
