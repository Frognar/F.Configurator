namespace F.Configurator.Engine.Tests;

// Analysis 2.5 and grammar 4.7: a value outside the limits triggers the reaction of the rule that gives the
// limit (`przy naruszeniu koryguj | błąd | ostrzeż "…"`), or the catalog's default reaction.
// - koryguj: the engine changes the value to the nearest allowed one and shows the message;
// - błąd: the value stays, the feature is marked, finishing is blocked;
// - ostrzeż: the value stays, the message is shown, finishing is allowed.
public class LimitReactionsTests
{
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(
        Func<CatalogBuilder, CatalogBuilder> addRules,
        Func<NumberFeatureBuilder, NumberFeatureBuilder>? width = null)
    {
        var catalog = addRules(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Number("SzerokoscMM", feature => (width ?? (w => w))(feature.Unit("mm")))
                .Collection("Basic", collection => collection
                    .Stage("Model", "Model")
                    .Stage("Wymiary", "SzerokoscMM")))
            .Build();

        return Configuration.Start(catalog, "Basic").Set("Model", Value.Option("PORTA"));
    }

    private static Func<CatalogBuilder, CatalogBuilder> MaxWidth(Reaction reaction) =>
        catalog => catalog.Rule("MaxSzerokosc", rule => rule
            .Max("SzerokoscMM", Number(1044m))
            .OnViolation(reaction, "Maksymalna szerokość to 1044 mm."));

    [Fact]
    public void Value_within_the_limits_violates_nothing()
    {
        var configuration = Start(MaxWidth(Reaction.Error)).Set("SzerokoscMM", Value.Number(900m));

        Assert.Empty(configuration.Violations);
        Assert.True(configuration.CanFinish);
    }

    [Fact]
    public void Correct_changes_the_value_to_the_limit()
    {
        var configuration = Start(MaxWidth(Reaction.Correct)).Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(Value.Number(1044m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal(
            [new RuleViolation("SzerokoscMM", "MaxSzerokosc", Reaction.Correct, "Maksymalna szerokość to 1044 mm.")],
            configuration.Violations);
    }

    [Fact]
    public void Correct_raises_the_value_to_the_minimum()
    {
        var configuration = Start(catalog => catalog.Rule("MinSzerokosc", rule => rule
                .Min("SzerokoscMM", Number(600m))
                .OnViolation(Reaction.Correct, "Minimalna szerokość to 600 mm.")))
            .Set("SzerokoscMM", Value.Number(500m));

        Assert.Equal(Value.Number(600m), configuration.ValueOf("SzerokoscMM"));
    }

    // Like `zaokrąglij(x, krok)`: the nearest multiple of the step, halves rounded up.
    [Theory]
    [InlineData(902, 900)]
    [InlineData(903, 905)]
    [InlineData(902.5, 905)]
    public void Correct_rounds_the_value_to_the_step(decimal entered, decimal expected)
    {
        var configuration = Start(catalog => catalog.Rule("KrokSzerokosci", rule => rule
                .Step("SzerokoscMM", Number(5m))
                .OnViolation(Reaction.Correct, "Szerokość co 5 mm.")))
            .Set("SzerokoscMM", Value.Number(entered));

        Assert.Equal(Value.Number(expected), configuration.ValueOf("SzerokoscMM"));
    }

    // The nearest allowed value lies within the limits and on the step, so a limit that is not a multiple of the
    // step gives the nearest multiple inside the limits. Only the broken limit is reported.
    [Fact]
    public void Correct_keeps_the_value_on_the_step_below_the_maximum()
    {
        var configuration = Start(catalog => MaxWidth(Reaction.Correct)(catalog)
                .Rule("KrokSzerokosci", rule => rule.Step("SzerokoscMM", Number(5m)).OnViolation(Reaction.Correct, "Szerokość co 5 mm.")))
            .Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(Value.Number(1040m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal(
            [new RuleViolation("SzerokoscMM", "MaxSzerokosc", Reaction.Correct, "Maksymalna szerokość to 1044 mm.")],
            configuration.Violations);
    }

    [Fact]
    public void Correct_keeps_the_value_on_the_step_above_the_minimum()
    {
        var configuration = Start(catalog => catalog
                .Rule("MinSzerokosc", rule => rule.Min("SzerokoscMM", Number(602m)).OnViolation(Reaction.Correct, "Minimalna szerokość to 602 mm."))
                .Rule("KrokSzerokosci", rule => rule.Step("SzerokoscMM", Number(5m)).OnViolation(Reaction.Correct, "Szerokość co 5 mm.")))
            .Set("SzerokoscMM", Value.Number(500m));

        Assert.Equal(Value.Number(605m), configuration.ValueOf("SzerokoscMM"));
    }

    // One correction fixes every limit the value broke, and each broken limit is reported once.
    [Fact]
    public void Correct_reports_every_limit_the_value_broke()
    {
        var configuration = Start(catalog => MaxWidth(Reaction.Correct)(catalog)
                .Rule("KrokSzerokosci", rule => rule.Step("SzerokoscMM", Number(5m)).OnViolation(Reaction.Correct, "Szerokość co 5 mm.")))
            .Set("SzerokoscMM", Value.Number(1103m));

        Assert.Equal(Value.Number(1040m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal(
            [
                new RuleViolation("SzerokoscMM", "KrokSzerokosci", Reaction.Correct, "Szerokość co 5 mm."),
                new RuleViolation("SzerokoscMM", "MaxSzerokosc", Reaction.Correct, "Maksymalna szerokość to 1044 mm."),
            ],
            configuration.Violations);
    }

    // The corrected value is still the user's choice, only adjusted.
    [Fact]
    public void Corrected_value_stays_the_users_value()
    {
        var configuration = Start(MaxWidth(Reaction.Correct)).Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(ValueSource.User, configuration.SourceOf("SzerokoscMM"));
    }

    // Analysis 3.2: limits follow the values they depend on, so a value can become invalid later.
    [Fact]
    public void Value_is_corrected_when_the_limit_changes()
    {
        var configuration = Start(catalog => catalog.Rule("MaxSzerokoscVerte", rule => rule
                .When(ModelVerte)
                .Max("SzerokoscMM", Number(1044m))
                .OnViolation(Reaction.Correct, "Verte ma maksymalnie 1044 mm.")))
            .Set("SzerokoscMM", Value.Number(1100m))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Number(1044m), configuration.ValueOf("SzerokoscMM"));
    }

    // Analysis 2.7: the step result carries the messages of that step; a correction is reported once.
    [Fact]
    public void Correction_is_reported_only_in_the_step_that_made_it()
    {
        var configuration = Start(MaxWidth(Reaction.Correct))
            .Set("SzerokoscMM", Value.Number(1100m))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Number(1044m), configuration.ValueOf("SzerokoscMM"));
        Assert.Empty(configuration.Violations);
    }

    [Fact]
    public void Error_keeps_the_value_and_blocks_finishing()
    {
        var configuration = Start(MaxWidth(Reaction.Error)).Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(Value.Number(1100m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal(
            [new RuleViolation("SzerokoscMM", "MaxSzerokosc", Reaction.Error, "Maksymalna szerokość to 1044 mm.")],
            configuration.Violations);
        Assert.False(configuration.CanFinish);
    }

    [Fact]
    public void Error_goes_when_the_value_is_fixed()
    {
        var configuration = Start(MaxWidth(Reaction.Error))
            .Set("SzerokoscMM", Value.Number(1100m))
            .Set("SzerokoscMM", Value.Number(1000m));

        Assert.Empty(configuration.Violations);
        Assert.True(configuration.CanFinish);
    }

    [Fact]
    public void Warning_keeps_the_value_and_allows_finishing()
    {
        var configuration = Start(MaxWidth(Reaction.Warning)).Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(Value.Number(1100m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal(
            [new RuleViolation("SzerokoscMM", "MaxSzerokosc", Reaction.Warning, "Maksymalna szerokość to 1044 mm.")],
            configuration.Violations);
        Assert.True(configuration.CanFinish);
    }

    // Grammar 4.1: a rule without `przy naruszeniu` uses the catalog's `reakcja`.
    [Fact]
    public void Rule_without_its_own_reaction_uses_the_catalog_reaction()
    {
        var configuration = Start(catalog => catalog
                .DefaultReaction(Reaction.Warning)
                .Rule("MaxSzerokosc", rule => rule.Max("SzerokoscMM", Number(1044m))))
            .Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal([new RuleViolation("SzerokoscMM", "MaxSzerokosc", Reaction.Warning, null)], configuration.Violations);
    }

    // Without a catalog reaction the safe choice is an error: nothing invalid is finished silently.
    [Fact]
    public void Without_any_reaction_a_violation_is_an_error()
    {
        var configuration = Start(catalog => catalog.Rule("MaxSzerokosc", rule => rule.Max("SzerokoscMM", Number(1044m))))
            .Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal([new RuleViolation("SzerokoscMM", "MaxSzerokosc", Reaction.Error, null)], configuration.Violations);
    }

    // A limit declared on the feature has no rule, so its violation names no rule and uses the catalog reaction.
    [Fact]
    public void Limit_declared_on_the_feature_reacts_with_the_catalog_reaction()
    {
        var configuration = Start(catalog => catalog.DefaultReaction(Reaction.Correct), width => width.Max(1200m))
            .Set("SzerokoscMM", Value.Number(1300m));

        Assert.Equal(Value.Number(1200m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal([new RuleViolation("SzerokoscMM", null, Reaction.Correct, null)], configuration.Violations);
    }

    // A configuration with a missing value cannot be finished either.
    [Fact]
    public void Configuration_with_a_missing_value_cannot_be_finished()
    {
        var configuration = Start(MaxWidth(Reaction.Error));

        Assert.False(configuration.CanFinish);
    }
}
