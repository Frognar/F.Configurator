namespace F.Configurator.Engine.Tests;

// Analysis 2.3: when several matching rules give a value to one feature, the highest priority wins
// (default 0; values from tables are the lowest). A tie with different values is a catalog error,
// never "first in the file wins", so the order of rules does not change the result.
public class PriorityTests
{
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(Func<CatalogBuilder, CatalogBuilder> addRules)
    {
        var catalog = addRules(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Number("SzerokoscMM", feature => feature.Unit("mm"))
                .Number("WysokoscMM", feature => feature.Unit("mm"))
                .Collection("Basic", collection => collection
                    .Stage("Model", "Model")
                    .Stage("Wymiary", "SzerokoscMM", "WysokoscMM")))
            .Build();

        return Configuration.Start(catalog, "Basic");
    }

    [Fact]
    public void Default_with_the_higher_priority_wins()
    {
        var configuration = Start(catalog => catalog
            .Rule("Wysokosc", rule => rule.Default("WysokoscMM", Number(2000m)))
            .Rule("WysokoscWyjatek", rule => rule.Priority(10).Default("WysokoscMM", Number(2050m))));

        Assert.Equal(Value.Number(2050m), configuration.ValueOf("WysokoscMM"));
    }

    [Fact]
    public void Default_with_the_higher_priority_wins_when_declared_first()
    {
        var configuration = Start(catalog => catalog
            .Rule("WysokoscWyjatek", rule => rule.Priority(10).Default("WysokoscMM", Number(2050m)))
            .Rule("Wysokosc", rule => rule.Default("WysokoscMM", Number(2000m))));

        Assert.Equal(Value.Number(2050m), configuration.ValueOf("WysokoscMM"));
    }

    // Analysis 2.3: an exception is a rule with a higher priority, not an "except Y" in the general rule.
    [Fact]
    public void Exception_applies_only_while_its_condition_holds()
    {
        var configuration = Start(catalog => catalog
                .Rule("Wysokosc", rule => rule.Default("WysokoscMM", Number(2000m)))
                .Rule("WysokoscVerte", rule => rule.When(ModelVerte).Priority(1).Default("WysokoscMM", Number(2050m))))
            .Set("Model", Value.Option("VERTE"))
            .Set("Model", Value.Option("PORTA"));

        Assert.Equal(Value.Number(2000m), configuration.ValueOf("WysokoscMM"));
    }

    [Fact]
    public void Tie_with_the_same_value_is_not_a_conflict()
    {
        var configuration = Start(catalog => catalog
            .Rule("WysokoscA", rule => rule.Default("WysokoscMM", Number(2000m)))
            .Rule("WysokoscB", rule => rule.Default("WysokoscMM", Number(2000m))));

        Assert.Equal(Value.Number(2000m), configuration.ValueOf("WysokoscMM"));
    }

    // Grammar 4.7: a tie with different values is a catalog error; it names the feature and the rules.
    [Fact]
    public void Tie_with_different_defaults_is_a_catalog_error()
    {
        var start = Start(catalog => catalog
            .Rule("WysokoscVerteA", rule => rule.When(ModelVerte).Default("WysokoscMM", Number(2000m)))
            .Rule("WysokoscVerteB", rule => rule.When(ModelVerte).Default("WysokoscMM", Number(2050m))));

        var error = Assert.Throws<InvalidOperationException>(() => start.Set("Model", Value.Option("VERTE")));
        Assert.Contains("'WysokoscMM'", error.Message);
        Assert.Contains("'WysokoscVerteA'", error.Message);
        Assert.Contains("'WysokoscVerteB'", error.Message);
    }

    // Defaults are never used for a feature the user has set, so their tie is not a conflict.
    [Fact]
    public void Tie_on_a_feature_set_by_the_user_is_not_a_conflict()
    {
        var configuration = Start(catalog => catalog
                .Rule("WysokoscVerteA", rule => rule.When(ModelVerte).Default("WysokoscMM", Number(2000m)))
                .Rule("WysokoscVerteB", rule => rule.When(ModelVerte).Default("WysokoscMM", Number(2050m))))
            .Set("Model", Value.Option("PORTA"))
            .Set("WysokoscMM", Value.Number(2100m))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Number(2100m), configuration.ValueOf("WysokoscMM"));
    }

    // Only the winning priority counts; a tie below it is never used.
    [Fact]
    public void Tie_below_the_winner_is_not_a_conflict()
    {
        var configuration = Start(catalog => catalog
            .Rule("WysokoscA", rule => rule.Default("WysokoscMM", Number(2000m)))
            .Rule("WysokoscB", rule => rule.Default("WysokoscMM", Number(2050m)))
            .Rule("WysokoscWyjatek", rule => rule.Priority(5).Default("WysokoscMM", Number(2100m))));

        Assert.Equal(Value.Number(2100m), configuration.ValueOf("WysokoscMM"));
    }

    // Analysis 2.3: values from tables have the lowest priority, so a plain rule overrides them.
    [Fact]
    public void Default_from_a_rule_overrides_a_default_from_a_table()
    {
        var wymiary = TableBuilder.Create(["Model"], ["SzerokoscMM", "WysokoscMM"])
            .Row([Value.Option("PORTA")], [Value.Number(844m), Value.Number(2000m)])
            .Build();

        var configuration = Start(catalog => catalog
                .Table("WymiaryDomyslne", wymiary)
                .Rule("Wymiary", rule => rule.DefaultsFrom(["SzerokoscMM", "WysokoscMM"], "WymiaryDomyslne"))
                .Rule("WysokoscPorta", rule => rule.Default("WysokoscMM", Number(2100m))))
            .Set("Model", Value.Option("PORTA"));

        Assert.Equal(Value.Number(844m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal(Value.Number(2100m), configuration.ValueOf("WysokoscMM"));
    }

    // Grammar 4.7: `ustaw` combines like `domyślnie`.
    [Fact]
    public void Forced_value_with_the_higher_priority_wins()
    {
        var configuration = Start(catalog => catalog
            .Rule("Wysokosc", rule => rule.Priority(3).Set("WysokoscMM", Number(2050m)))
            .Rule("WysokoscOgolna", rule => rule.Set("WysokoscMM", Number(2000m))));

        Assert.Equal(Value.Number(2050m), configuration.ValueOf("WysokoscMM"));
    }

    [Fact]
    public void Tie_with_different_forced_values_is_a_catalog_error()
    {
        var start = Start(catalog => catalog
            .Rule("WysokoscVerteA", rule => rule.When(ModelVerte).Set("WysokoscMM", Number(2000m)))
            .Rule("WysokoscVerteB", rule => rule.When(ModelVerte).Set("WysokoscMM", Number(2050m))));

        var error = Assert.Throws<InvalidOperationException>(() => start.Set("Model", Value.Option("VERTE")));
        Assert.Contains("'WysokoscMM'", error.Message);
    }
}
