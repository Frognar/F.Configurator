namespace F.Configurator.Engine.Tests;

// Analysis 2.3: the order of the rules in the files does not matter. When several rules do the same to a feature, the
// values do not depend on their order, and neither does the rule the engine names nor its reaction. Decided
// 2026-10-13: the higher priority wins, then the most severe reaction (error, warning, correction), then the rule
// name in alphabetical order. Messages go from errors to information, by rule name within a kind.
public class RuleTiesTests
{
    private static readonly Expression ModelPorta = Equal(Reference("Model"), Option("PORTA"));
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));
    private static readonly Expression ModelLux = Equal(Reference("Model"), Option("LUX"));

    // Each test declares its rules in both orders.
    private static Configuration Start(bool reversed, params Func<CatalogBuilder, CatalogBuilder>[] rules)
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature
                .Option("PORTA", "Porta")
                .Option("VERTE", "Verte")
                .Option("LUX", "Lux"))
            .Choice("Kolor", feature => feature.Option("BIALY", "Biały").Option("DAB", "Dąb"))
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Text("Napis")
            .Collection("Basic", collection => collection
                .Stage("Model", "Model")
                .Stage("Wykonczenie", "Kolor", "SzerokoscMM", "Napis"));

        return Configuration.Start(
            (reversed ? rules.Reverse() : rules).Aggregate(catalog, (all, addRule) => addRule(all)).Build(),
            "Basic");
    }

    private static CatalogBuilder VerteWithoutOakIsAnError(CatalogBuilder catalog) =>
        catalog.Rule("VerteBezDebu", rule => rule
            .When(ModelVerte)
            .Forbid("Kolor", "DAB")
            .OnViolation(Reaction.Error, "Verte nie ma koloru dąb."));

    private static CatalogBuilder OakOnlyForPortaIsCorrected(CatalogBuilder catalog) =>
        catalog.Rule("DabTylkoDoPorty", rule => rule
            .When(ModelVerte)
            .Forbid("Kolor", "DAB")
            .OnViolation(Reaction.Correct, "Dąb jest tylko do Porty."));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Option_excluded_by_several_rules_names_the_most_severe(bool reversed)
    {
        var configuration = Start(reversed, OakOnlyForPortaIsCorrected, VerteWithoutOakIsAnError)
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal([new UnavailableOption("DAB", "VerteBezDebu")], configuration.UnavailableOptions("Kolor"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Chosen_option_excluded_by_several_rules_reacts_as_the_most_severe(bool reversed)
    {
        var configuration = Start(reversed, OakOnlyForPortaIsCorrected, VerteWithoutOakIsAnError)
            .Set("Model", Value.Option("PORTA"))
            .Set("Kolor", Value.Option("DAB"))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Option("DAB"), configuration.ValueOf("Kolor"));
        Assert.Equal(
            [new RuleViolation("Kolor", "VerteBezDebu", Reaction.Error, "Verte nie ma koloru dąb.")],
            configuration.Violations);
    }

    // An exclusion does not depend on priority, since an option is out if any rule excludes it; the priority still
    // picks the rule that explains it and reacts.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Higher_priority_wins_over_a_more_severe_reaction(bool reversed)
    {
        var configuration = Start(
                reversed,
                VerteWithoutOakIsAnError,
                catalog => catalog.Rule("DabTylkoDoPorty", rule => rule
                    .When(ModelVerte)
                    .Priority(1)
                    .Forbid("Kolor", "DAB")
                    .OnViolation(Reaction.Correct, "Dąb jest tylko do Porty.")))
            .Set("Model", Value.Option("PORTA"))
            .Set("Kolor", Value.Option("DAB"))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Kolor"));
        Assert.Equal(
            [new RuleViolation("Kolor", "DabTylkoDoPorty", Reaction.Correct, "Dąb jest tylko do Porty.")],
            configuration.Violations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rules_with_the_same_reaction_are_named_by_priority(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("BrakDebuDlaVerte", rule => rule.When(ModelVerte).Forbid("Kolor", "DAB")),
                catalog => catalog.Rule("VerteBezDebu", rule => rule
                    .When(ModelVerte)
                    .Priority(1)
                    .Forbid("Kolor", "DAB")))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal([new UnavailableOption("DAB", "VerteBezDebu")], configuration.UnavailableOptions("Kolor"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rules_with_the_same_reaction_and_priority_are_named_alphabetically(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("VerteBezDebu", rule => rule.When(ModelVerte).Forbid("Kolor", "DAB")),
                catalog => catalog.Rule("BrakDebuDlaVerte", rule => rule.When(ModelVerte).Forbid("Kolor", "DAB")))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal([new UnavailableOption("DAB", "BrakDebuDlaVerte")], configuration.UnavailableOptions("Kolor"));
    }

    // Both limits are 1000 mm, so the more restrictive one cannot decide; the warning is more severe than the
    // correction, so the width the user entered stays.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Equal_limits_react_as_the_most_severe(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("MaksymalnaSzerokosc", rule => rule
                    .Max("SzerokoscMM", Number(1000m))
                    .OnViolation(Reaction.Correct, "Maksymalna szerokość to 1000 mm.")),
                catalog => catalog.Rule("SzerokoscProdukcyjna", rule => rule
                    .Max("SzerokoscMM", Number(1000m))
                    .OnViolation(Reaction.Warning, "Szerokość ponad 1000 mm wymaga zgody produkcji.")))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(Value.Number(1100m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal(
            [
                new RuleViolation(
                    "SzerokoscMM",
                    "SzerokoscProdukcyjna",
                    Reaction.Warning,
                    "Szerokość ponad 1000 mm wymaga zgody produkcji.")
            ],
            configuration.Violations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Equal_limits_with_the_same_reaction_are_named_alphabetically(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("SzerokoscProdukcyjna", rule => rule.Max("SzerokoscMM", Number(1000m))),
                catalog => catalog.Rule("MaksymalnaSzerokosc", rule => rule.Max("SzerokoscMM", Number(1000m))))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(
            [new RuleViolation("SzerokoscMM", "MaksymalnaSzerokosc", Reaction.Error, null)],
            configuration.Violations);
    }

    // Steps of 2 and 5 mm combine into 10 mm, which no single rule gives, so the reaction picks the rule.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Combined_step_reacts_as_the_most_severe(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("KrokDwa", rule => rule
                    .Step("SzerokoscMM", Number(2m))
                    .OnViolation(Reaction.Warning, "Szerokość co 2 mm.")),
                catalog => catalog.Rule("KrokPiec", rule => rule
                    .Step("SzerokoscMM", Number(5m))
                    .OnViolation(Reaction.Correct, "Szerokość co 5 mm.")))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(1003m));

        Assert.Equal(Value.Number(1003m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal(
            [new RuleViolation("SzerokoscMM", "KrokDwa", Reaction.Warning, "Szerokość co 2 mm.")],
            configuration.Violations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Combined_step_with_the_same_reaction_is_named_alphabetically(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("KrokPiec", rule => rule.Step("SzerokoscMM", Number(5m))),
                catalog => catalog.Rule("KrokDwa", rule => rule.Step("SzerokoscMM", Number(2m))))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(1003m));

        Assert.Equal([new RuleViolation("SzerokoscMM", "KrokDwa", Reaction.Error, null)], configuration.Violations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Equal_lengths_react_as_the_most_severe(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("NapisKrotki", rule => rule
                    .Length("Napis", Range(1m, 5m))
                    .OnViolation(Reaction.Correct, "Napis ma do 5 znaków.")),
                catalog => catalog.Rule("NapisNaDrzwiach", rule => rule
                    .Length("Napis", Range(1m, 5m))
                    .OnViolation(Reaction.Warning, "Dłuższy napis może się nie zmieścić.")))
            .Set("Model", Value.Option("PORTA"))
            .Set("Napis", Value.Text("WEJSCIE"));

        Assert.Equal(Value.Text("WEJSCIE"), configuration.ValueOf("Napis"));
        Assert.Equal(
            [new RuleViolation("Napis", "NapisNaDrzwiach", Reaction.Warning, "Dłuższy napis może się nie zmieścić.")],
            configuration.Violations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Equal_lengths_with_the_same_reaction_are_named_alphabetically(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("NapisNaDrzwiach", rule => rule.Length("Napis", Range(1m, 5m))),
                catalog => catalog.Rule("NapisKrotki", rule => rule.Length("Napis", Range(1m, 5m))))
            .Set("Model", Value.Option("PORTA"))
            .Set("Napis", Value.Text("WEJSCIE"));

        Assert.Equal([new RuleViolation("Napis", "NapisKrotki", Reaction.Error, null)], configuration.Violations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Equal_defaults_are_named_alphabetically(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("KolorStandardowy", rule => rule
                    .When(ModelPorta)
                    .Default("Kolor", Option("BIALY"))),
                catalog => catalog.Rule("KolorBialy", rule => rule.When(ModelPorta).Default("Kolor", Option("BIALY"))))
            .Set("Model", Value.Option("PORTA"));

        Assert.Equal(
            [new ValueChange("Kolor", Value.Missing, Value.Option("BIALY"), ChangeCause.Default, "KolorBialy")],
            configuration.Changes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Feature_hidden_by_several_rules_is_named_alphabetically(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("LuxUkryjKolor", rule => rule.When(ModelLux).Hide("Kolor")),
                catalog => catalog.Rule("LuxBezKoloru", rule => rule.When(ModelLux).Hide("Kolor")))
            .Set("Model", Value.Option("PORTA"))
            .Set("Kolor", Value.Option("DAB"))
            .Set("Model", Value.Option("LUX"));

        Assert.Equal(
            [new ValueChange("Kolor", Value.Option("DAB"), Value.Missing, ChangeCause.Hidden, "LuxBezKoloru")],
            configuration.Changes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Messages_go_from_errors_to_information_and_by_rule_name(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("VerteInformacja", rule => rule
                    .When(ModelVerte)
                    .Inform("Verte ma ukryte zawiasy.")),
                catalog => catalog.Rule("VerteWycofana", rule => rule.When(ModelVerte).Error("Verte jest wycofana.")),
                catalog => catalog.Rule("VerteTermin", rule => rule.When(ModelVerte).Warn("Verte ma dłuższy termin.")),
                catalog => catalog.Rule("VerteCena", rule => rule.When(ModelVerte).Warn("Verte ma nową cenę.")))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(
            [
                new StepMessage(MessageKind.Error, "Verte jest wycofana.", "VerteWycofana", null),
                new StepMessage(MessageKind.Warning, "Verte ma nową cenę.", "VerteCena", null),
                new StepMessage(MessageKind.Warning, "Verte ma dłuższy termin.", "VerteTermin", null),
                new StepMessage(MessageKind.Information, "Verte ma ukryte zawiasy.", "VerteInformacja", null),
            ],
            configuration.Result.Messages);
    }

    // A correction is an information, so it comes after the warning of a rule.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Corrections_take_their_place_among_the_messages(bool reversed)
    {
        var configuration = Start(
                reversed,
                catalog => catalog.Rule("MaksymalnaSzerokosc", rule => rule
                    .Max("SzerokoscMM", Number(1000m))
                    .OnViolation(Reaction.Correct, "Maksymalna szerokość to 1000 mm.")),
                catalog => catalog.Rule("PortaTermin", rule => rule.When(ModelPorta).Warn("Porta ma dłuższy termin.")))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(
            [
                new StepMessage(MessageKind.Warning, "Porta ma dłuższy termin.", "PortaTermin", null),
                new StepMessage(
                    MessageKind.Information, "Maksymalna szerokość to 1000 mm.", "MaksymalnaSzerokosc", "SzerokoscMM"),
            ],
            configuration.Result.Messages);
    }
}
