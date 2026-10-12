namespace F.Configurator.Engine.Tests;

// Analysis 2.3 and grammar 4.7: `min A = …`, `maks A = …` and `krok A = …` limit a number while the rule's
// condition holds. Limits declared on the feature are fixed ones; rules give the dependent ones. The highest
// priority wins; on a tie the most restrictive limit wins, so independent limits work together.
public class NumberLimitsTests
{
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(
        Func<CatalogBuilder, CatalogBuilder> addRules,
        Func<NumberFeatureBuilder, NumberFeatureBuilder>? width = null)
    {
        var catalog = addRules(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Number("SzerokoscMM", feature => (width ?? (w => w))(feature.Unit("mm")))
                .Number("WysokoscMM", feature => feature.Unit("mm"))
                .Collection("Basic", collection => collection
                    .Stage("Model", "Model")
                    .Stage("Wymiary", "SzerokoscMM", "WysokoscMM")))
            .Build();

        return Configuration.Start(catalog, "Basic");
    }

    [Fact]
    public void Number_without_limits_has_none()
    {
        var configuration = Start(catalog => catalog);

        Assert.Equal(new NumberLimits(null, null, null), configuration.LimitsOf("SzerokoscMM"));
    }

    [Fact]
    public void Limits_declared_on_the_feature_apply()
    {
        var configuration = Start(catalog => catalog, width => width.Min(600m).Max(1200m).Step(5m));

        Assert.Equal(new NumberLimits(600m, 1200m, 5m), configuration.LimitsOf("SzerokoscMM"));
    }

    [Fact]
    public void Rule_gives_limits()
    {
        var configuration = Start(catalog => catalog
            .Rule("SzerokoscBasic", rule => rule
                .Min("SzerokoscMM", Number(600m))
                .Max("SzerokoscMM", Number(1044m))
                .Step("SzerokoscMM", Number(2m))));

        Assert.Equal(new NumberLimits(600m, 1044m, 2m), configuration.LimitsOf("SzerokoscMM"));
    }

    [Fact]
    public void Limit_applies_only_while_its_condition_holds()
    {
        var start = Start(catalog => catalog
            .Rule("SzerokoscVerte", rule => rule.When(ModelVerte).Max("SzerokoscMM", Number(1044m))));

        Assert.Equal(1044m, start.Set("Model", Value.Option("VERTE")).LimitsOf("SzerokoscMM").Max);
        Assert.Null(start.Set("Model", Value.Option("PORTA")).LimitsOf("SzerokoscMM").Max);
    }

    // A limit is an expression, so it may use other values.
    [Fact]
    public void Limit_is_computed_from_other_values()
    {
        var configuration = Start(catalog => catalog
                .Rule("WysokoscDoSzerokosci", rule => rule.Max("WysokoscMM", Add(Reference("SzerokoscMM"), Number(1200m)))))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(900m));

        Assert.Equal(2100m, configuration.LimitsOf("WysokoscMM").Max);
    }

    // Grammar 6.4: an expression on a feature without a value gives no value, so it gives no limit.
    [Fact]
    public void Limit_without_a_value_gives_no_limit()
    {
        var configuration = Start(catalog => catalog
            .Rule("WysokoscDoSzerokosci", rule => rule.Max("WysokoscMM", Add(Reference("SzerokoscMM"), Number(1200m)))));

        Assert.Null(configuration.LimitsOf("WysokoscMM").Max);
    }

    // Like values from tables, limits declared on the feature have the lowest priority, so a rule replaces them.
    [Fact]
    public void Limit_from_a_rule_replaces_the_limit_declared_on_the_feature()
    {
        var configuration = Start(
            catalog => catalog.Rule("SzerokoscSzeroka", rule => rule.Max("SzerokoscMM", Number(1300m))),
            width => width.Min(600m).Max(1200m));

        Assert.Equal(new NumberLimits(600m, 1300m, null), configuration.LimitsOf("SzerokoscMM"));
    }

    // Analysis 2.3: an exception is a rule with a higher priority; it wins even when it is less restrictive.
    [Fact]
    public void Limit_with_the_higher_priority_wins()
    {
        var configuration = Start(catalog => catalog
            .Rule("MaxSzerBasic", rule => rule.Max("SzerokoscMM", Number(1044m)))
            .Rule("MaxSzerY", rule => rule.Priority(10).Max("SzerokoscMM", Number(1100m)))
            .Rule("KrokOgolny", rule => rule.Step("SzerokoscMM", Number(2m)))
            .Rule("KrokY", rule => rule.Priority(10).Step("SzerokoscMM", Number(5m))));

        Assert.Equal(1100m, configuration.LimitsOf("SzerokoscMM").Max);
        Assert.Equal(5m, configuration.LimitsOf("SzerokoscMM").Step);
    }

    // Analysis 2.3: two independent limits (e.g. a group limit and a rebate limit) work together.
    [Fact]
    public void Tie_takes_the_most_restrictive_limit()
    {
        var configuration = Start(catalog => catalog
            .Rule("LimitGrupy", rule => rule.Min("SzerokoscMM", Number(600m)).Max("SzerokoscMM", Number(1044m)))
            .Rule("LimitFelcu", rule => rule.Min("SzerokoscMM", Number(700m)).Max("SzerokoscMM", Number(1100m))));

        Assert.Equal(700m, configuration.LimitsOf("SzerokoscMM").Min);
        Assert.Equal(1044m, configuration.LimitsOf("SzerokoscMM").Max);
    }

    // Two independent steps work together: only values that keep both are left, so the step is their least
    // common multiple (decided 2026-10-12).
    [Theory]
    [InlineData(2, 5, 10)]
    [InlineData(2, 10, 10)]
    [InlineData(0.5, 0.2, 1)]
    [InlineData(0.3, 0.2, 0.6)]
    public void Tie_of_steps_takes_their_least_common_multiple(decimal first, decimal second, decimal expected)
    {
        var configuration = Start(catalog => catalog
            .Rule("KrokA", rule => rule.Step("SzerokoscMM", Number(first)))
            .Rule("KrokB", rule => rule.Step("SzerokoscMM", Number(second))));

        Assert.Equal(expected, configuration.LimitsOf("SzerokoscMM").Step);
    }

    // Like `zaokrąglij(x, krok)`, a step that is not positive gives nothing, so other steps still apply.
    [Fact]
    public void Step_that_is_not_positive_gives_no_step()
    {
        var configuration = Start(
            catalog => catalog.Rule("KrokZero", rule => rule.Step("SzerokoscMM", Number(0m))),
            width => width.Step(5m));

        Assert.Equal(5m, configuration.LimitsOf("SzerokoscMM").Step);
    }

    // A rule whose limit has no value gives no limit, so it does not hide the limits of lower priority.
    [Fact]
    public void Rule_without_a_value_leaves_the_limit_of_lower_priority()
    {
        var configuration = Start(catalog => catalog
            .Rule("MaxWysokosc", rule => rule.Max("WysokoscMM", Number(2100m)))
            .Rule("WysokoscDoSzerokosci", rule => rule
                .Priority(10)
                .Max("WysokoscMM", Add(Reference("SzerokoscMM"), Number(1200m)))));

        Assert.Equal(2100m, configuration.LimitsOf("WysokoscMM").Max);
    }

    [Fact]
    public void Limits_of_a_feature_that_is_not_a_number_are_rejected()
    {
        var configuration = Start(catalog => catalog);

        Assert.Throws<ArgumentException>(() => configuration.LimitsOf("Model"));
        Assert.Throws<ArgumentException>(() => configuration.LimitsOf("Kolor"));
    }
}
