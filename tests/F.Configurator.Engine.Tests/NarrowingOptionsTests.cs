namespace F.Configurator.Engine.Tests;

// Analysis 2.3 and grammar 4.7: `tylko` lists the allowed options and `zabroń` the forbidden ones.
// Options left = intersection of all matching `tylko` minus the union of all matching `zabroń`.
// For every option left out the configurator tells which rule excludes it.
public class NarrowingOptionsTests
{
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(Func<CatalogBuilder, CatalogBuilder> addRules)
    {
        var catalog = addRules(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Choice("Norma", feature => feature.Option("PL", "Polska").Option("CZ", "Czeska").Option("DE", "Niemiecka"))
                .Collection("Basic", collection => collection.Stage("Drzwi", "Model", "Norma")))
            .Build();

        return Configuration.Start(catalog, "Basic");
    }

    [Fact]
    public void Without_rules_all_options_are_available_in_declaration_order()
    {
        var configuration = Start(catalog => catalog);

        Assert.Equal(["PL", "CZ", "DE"], configuration.AvailableOptions("Norma"));
        Assert.Empty(configuration.UnavailableOptions("Norma"));
    }

    [Fact]
    public void Only_leaves_the_listed_options()
    {
        var configuration = Start(catalog => catalog.Rule("TylkoPL", rule => rule.Only("Norma", "PL", "DE")));

        Assert.Equal(["PL", "DE"], configuration.AvailableOptions("Norma"));
    }

    [Fact]
    public void Several_only_rules_leave_the_options_they_share()
    {
        var configuration = Start(catalog => catalog
            .Rule("BezDE", rule => rule.Only("Norma", "PL", "CZ"))
            .Rule("BezCZ", rule => rule.Only("Norma", "PL", "DE")));

        Assert.Equal(["PL"], configuration.AvailableOptions("Norma"));
    }

    [Fact]
    public void Forbid_removes_the_listed_options()
    {
        var configuration = Start(catalog => catalog.Rule("BezCZ", rule => rule.Forbid("Norma", "CZ")));

        Assert.Equal(["PL", "DE"], configuration.AvailableOptions("Norma"));
    }

    [Fact]
    public void Several_forbid_rules_remove_all_their_options()
    {
        var configuration = Start(catalog => catalog
            .Rule("BezCZ", rule => rule.Forbid("Norma", "CZ"))
            .Rule("BezDE", rule => rule.Forbid("Norma", "DE")));

        Assert.Equal(["PL"], configuration.AvailableOptions("Norma"));
    }

    [Fact]
    public void Forbid_removes_options_that_only_allows()
    {
        var configuration = Start(catalog => catalog
            .Rule("BezDE", rule => rule.Only("Norma", "PL", "CZ"))
            .Rule("BezCZ", rule => rule.Forbid("Norma", "CZ")));

        Assert.Equal(["PL"], configuration.AvailableOptions("Norma"));
    }

    [Fact]
    public void Rule_that_does_not_match_narrows_nothing()
    {
        var configuration = Start(catalog => catalog.Rule("VerteBezCZ", rule => rule.When(ModelVerte).Forbid("Norma", "CZ")))
            .Set("Model", Value.Option("PORTA"));

        Assert.Equal(["PL", "CZ", "DE"], configuration.AvailableOptions("Norma"));
    }

    [Fact]
    public void Options_narrow_after_the_value_the_rule_depends_on_is_set()
    {
        var configuration = Start(catalog => catalog.Rule("VerteBezCZ", rule => rule.When(ModelVerte).Forbid("Norma", "CZ")))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(["PL", "DE"], configuration.AvailableOptions("Norma"));
    }

    // Analysis 3.2: for an unavailable option the state keeps the rule that excludes it.
    [Fact]
    public void Unavailable_option_names_the_rule_that_forbids_it()
    {
        var configuration = Start(catalog => catalog.Rule("BezCZ", rule => rule.Forbid("Norma", "CZ")));

        Assert.Equal([new UnavailableOption("CZ", "BezCZ")], configuration.UnavailableOptions("Norma"));
    }

    [Fact]
    public void Unavailable_options_name_the_rule_that_leaves_them_out()
    {
        var configuration = Start(catalog => catalog.Rule("TylkoPL", rule => rule.Only("Norma", "PL")));

        Assert.Equal(
            [new UnavailableOption("CZ", "TylkoPL"), new UnavailableOption("DE", "TylkoPL")],
            configuration.UnavailableOptions("Norma"));
    }

    // Unlike an option the feature does not have, an unavailable option is valid in another state.
    [Fact]
    public void Setting_an_unavailable_option_is_rejected()
    {
        var configuration = Start(catalog => catalog.Rule("BezCZ", rule => rule.Forbid("Norma", "CZ")));

        Assert.Throws<InvalidOperationException>(() => configuration.Set("Norma", Value.Option("CZ")));
    }

    [Fact]
    public void Options_of_an_unknown_feature_are_rejected()
    {
        var configuration = Start(catalog => catalog);

        Assert.Throws<ArgumentException>(() => configuration.AvailableOptions("Kolor"));
    }
}
