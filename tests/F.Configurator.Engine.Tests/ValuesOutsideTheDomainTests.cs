namespace F.Configurator.Engine.Tests;

// Analysis 3.3: no visible value lies outside its domain without a message. Analysis 3.2 settles the domain before
// the value, so a default picks from what the rules allow. Decided 2026-10-13: a default outside the domain is not
// used, and a lower default or no value takes its place; a forced value cannot be skipped, so outside the domain it
// is an error of the rule that limits it.
public class ValuesOutsideTheDomainTests
{
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(Func<CatalogBuilder, CatalogBuilder> addRules)
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
            .Choice("Kolor", feature => feature.Option("BIALY", "Biały").Option("DAB", "Dąb"))
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Text("Napis")
            .Collection("Basic", collection => collection
                .Stage("Model", "Model")
                .Stage("Wykonczenie", "Kolor", "SzerokoscMM", "Napis"));

        return Configuration.Start(addRules(catalog).Build(), "Basic");
    }

    private static CatalogBuilder VerteWithoutOak(CatalogBuilder catalog) =>
        catalog.Rule("VerteBezDebu", rule => rule
            .When(ModelVerte)
            .Forbid("Kolor", "DAB")
            .OnViolation(Reaction.Correct, "Verte nie ma koloru dąb."));

    [Fact]
    public void Default_option_the_rules_exclude_is_not_used()
    {
        var configuration = Start(catalog => VerteWithoutOak(catalog)
                .Rule("KolorDab", rule => rule.Default("Kolor", Option("DAB"))))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Kolor"));
    }

    [Fact]
    public void Lower_default_takes_the_place_of_an_excluded_one()
    {
        var configuration = Start(catalog => VerteWithoutOak(catalog)
                .Rule("KolorDab", rule => rule.Priority(1).Default("Kolor", Option("DAB")))
                .Rule("KolorBialy", rule => rule.Default("Kolor", Option("BIALY"))))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Option("BIALY"), configuration.ValueOf("Kolor"));
    }

    [Fact]
    public void Default_number_outside_the_limits_is_not_used()
    {
        var configuration = Start(catalog => catalog
                .Rule("SzerokoscDomyslna", rule => rule.Default("SzerokoscMM", Number(900m)))
                .Rule("VerteWaska", rule => rule.When(ModelVerte).Max("SzerokoscMM", Number(800m))))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Missing, configuration.ValueOf("SzerokoscMM"));
    }

    [Fact]
    public void Default_number_off_the_step_is_not_used()
    {
        var configuration = Start(catalog => catalog
                .Rule("SzerokoscDomyslna", rule => rule.Default("SzerokoscMM", Number(905m)))
                .Rule("VerteCoDziesiec", rule => rule.When(ModelVerte).Step("SzerokoscMM", Number(10m))))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Missing, configuration.ValueOf("SzerokoscMM"));
    }

    [Fact]
    public void Default_text_of_a_wrong_length_is_not_used()
    {
        var configuration = Start(catalog => catalog
                .Rule("NapisDomyslny", rule => rule.Default("Napis", Text("Drzwi wejściowe")))
                .Rule("VerteKrotkiNapis", rule => rule.When(ModelVerte).Length("Napis", Range(1m, 5m))))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Napis"));
    }

    // The forced value stays, since the rule that forces it wins over the user, but the catalog contradicts itself, so
    // the configuration cannot be finished until the catalog is fixed.
    [Fact]
    public void Forced_option_the_rules_exclude_is_an_error()
    {
        var configuration = Start(catalog => VerteWithoutOak(catalog)
                .Rule("VerteDab", rule => rule.When(ModelVerte).Set("Kolor", Option("DAB"))))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Option("DAB"), configuration.ValueOf("Kolor"));
        Assert.Equal(
            [new RuleViolation("Kolor", "VerteBezDebu", Reaction.Error, "Verte nie ma koloru dąb.")],
            configuration.Violations);
    }

    [Fact]
    public void Forced_number_outside_the_limits_is_an_error()
    {
        var configuration = Start(catalog => catalog
                .Rule("VerteSzeroka", rule => rule.When(ModelVerte).Set("SzerokoscMM", Number(1100m)))
                .Rule("MaksymalnaSzerokosc", rule => rule
                    .Max("SzerokoscMM", Number(1000m))
                    .OnViolation(Reaction.Correct, "Maksymalna szerokość to 1000 mm.")))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Number(1100m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal(
            [new RuleViolation("SzerokoscMM", "MaksymalnaSzerokosc", Reaction.Error, "Maksymalna szerokość to 1000 mm.")],
            configuration.Violations);
    }

    [Fact]
    public void Forced_text_of_a_wrong_length_is_an_error()
    {
        var configuration = Start(catalog => catalog
                .Rule("VerteNapis", rule => rule.When(ModelVerte).Set("Napis", Text("Drzwi wejściowe")))
                .Rule("KrotkiNapis", rule => rule
                    .Length("Napis", Range(1m, 5m))
                    .OnViolation(Reaction.Correct, "Napis ma do 5 znaków.")))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Text("Drzwi wejściowe"), configuration.ValueOf("Napis"));
        Assert.Equal(
            [new RuleViolation("Napis", "KrotkiNapis", Reaction.Error, "Napis ma do 5 znaków.")],
            configuration.Violations);
    }
}
