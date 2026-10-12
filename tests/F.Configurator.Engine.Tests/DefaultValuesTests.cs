namespace F.Configurator.Engine.Tests;

// Analysis 2.3 and 3.2, grammar 4.7: `domyślnie A = …` gives a value the user has not chosen; it follows
// the values it depends on until the user sets the feature. `ustaw A = …` forces a value and makes the
// feature read-only. Every value keeps its source.
public class DefaultValuesTests
{
    private static readonly Expression ModelPorta = Equal(Reference("Model"), Option("PORTA"));
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(Func<CatalogBuilder, CatalogBuilder> addRules)
    {
        var catalog = addRules(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Choice("Norma", feature => feature.Option("PL", "Polska").Option("CZ", "Czeska"))
                .Number("SzerokoscMM", feature => feature.Unit("mm"))
                .Number("WysokoscMM", feature => feature.Unit("mm"))
                .Collection("Basic", collection => collection
                    .Stage("Model", "Model", "Norma")
                    .Stage("Wymiary", "SzerokoscMM", "WysokoscMM")))
            .Build();

        return Configuration.Start(catalog, "Basic");
    }

    private static Configuration StartWithNormaPl() =>
        Start(catalog => catalog.Rule("NormaPL", rule => rule.Default("Norma", Option("PL"))));

    private static Configuration StartWithHeightByModel() =>
        Start(catalog => catalog
            .Rule("NormaPL", rule => rule.Default("Norma", Option("PL")))
            .Rule("WysokoscPorta", rule => rule.When(ModelPorta).Default("WysokoscMM", Number(2000m)))
            .Rule("WysokoscVerte", rule => rule.When(ModelVerte).Default("WysokoscMM", Number(2050m))));

    [Fact]
    public void Default_becomes_the_value()
    {
        var configuration = StartWithNormaPl();

        Assert.Equal(Value.Option("PL"), configuration.ValueOf("Norma"));
        Assert.Equal(ValueSource.Default, configuration.SourceOf("Norma"));
    }

    [Fact]
    public void Value_set_by_the_user_has_the_user_as_its_source()
    {
        var configuration = StartWithNormaPl().Set("Norma", Value.Option("CZ"));

        Assert.Equal(Value.Option("CZ"), configuration.ValueOf("Norma"));
        Assert.Equal(ValueSource.User, configuration.SourceOf("Norma"));
    }

    [Fact]
    public void Feature_without_a_value_has_no_source()
    {
        var configuration = StartWithNormaPl();

        Assert.Null(configuration.SourceOf("Model"));
    }

    [Fact]
    public void Default_waits_for_its_condition()
    {
        var configuration = StartWithHeightByModel().Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Number(2050m), configuration.ValueOf("WysokoscMM"));
    }

    // Analysis 3.2: a value not from the user takes the new default when what it depends on changes.
    [Fact]
    public void Default_follows_the_values_it_depends_on()
    {
        var configuration = StartWithHeightByModel()
            .Set("Model", Value.Option("PORTA"))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Number(2050m), configuration.ValueOf("WysokoscMM"));
    }

    [Fact]
    public void Value_from_the_user_stays_when_the_default_changes()
    {
        var configuration = StartWithHeightByModel()
            .Set("Model", Value.Option("PORTA"))
            .Set("WysokoscMM", Value.Number(2100m))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Number(2100m), configuration.ValueOf("WysokoscMM"));
        Assert.Equal(ValueSource.User, configuration.SourceOf("WysokoscMM"));
    }

    [Fact]
    public void Default_disappears_when_its_rule_stops_matching()
    {
        var configuration = Start(catalog => catalog
                .Rule("WysokoscVerte", rule => rule.When(ModelVerte).Default("WysokoscMM", Number(2050m))))
            .Set("Model", Value.Option("VERTE"))
            .Set("Model", Value.Option("PORTA"));

        Assert.Equal(Value.Missing, configuration.ValueOf("WysokoscMM"));
    }

    // A default is an expression, so it may use other values.
    [Fact]
    public void Default_is_computed_from_other_values()
    {
        var configuration = Start(catalog => catalog
                .Rule("NormaPL", rule => rule.Default("Norma", Option("PL")))
                .Rule("Wysokosc", rule => rule.Default("WysokoscMM", Add(Reference("SzerokoscMM"), Number(1200m)))))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(900m));

        Assert.Equal(Value.Number(2100m), configuration.ValueOf("WysokoscMM"));
    }

    // A feature with a default already has a value, so it does not hold up its stage.
    [Fact]
    public void Defaults_complete_a_stage()
    {
        var configuration = StartWithNormaPl().Set("Model", Value.Option("PORTA"));

        Assert.Equal("Wymiary", configuration.CurrentStage);
    }

    // Grammar 6.4: a hidden feature behaves as unset, defaults included.
    [Fact]
    public void Hidden_feature_gets_no_default()
    {
        var configuration = Start(catalog => catalog
                .Rule("NormaPL", rule => rule.Default("Norma", Option("PL")))
                .Rule("VerteBezNormy", rule => rule.When(ModelVerte).Hide("Norma")))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Norma"));
    }

    // Grammar 4.4: `domyślnie SzerokoscMM, WysokoscMM z WymiaryDomyslne`; the table's key columns are
    // features and its value columns are matched with the listed features by name.
    [Fact]
    public void Defaults_come_from_the_table_row_matching_the_key()
    {
        var configuration = StartWithDimensionsTable()
            .Set("Model", Value.Option("PORTA"))
            .Set("Norma", Value.Option("CZ"));

        Assert.Equal(Value.Number(800m), configuration.ValueOf("SzerokoscMM"));
        Assert.Equal(Value.Number(1970m), configuration.ValueOf("WysokoscMM"));
        Assert.Equal(ValueSource.Default, configuration.SourceOf("WysokoscMM"));
    }

    [Fact]
    public void Table_gives_no_defaults_while_its_key_has_no_value()
    {
        var configuration = StartWithDimensionsTable();

        Assert.Equal(Value.Missing, configuration.ValueOf("SzerokoscMM"));
    }

    // Grammar 4.7: `ustaw` forces the value, and the feature is read-only.
    [Fact]
    public void Forced_value_becomes_the_value_and_locks_the_feature()
    {
        var configuration = Start(catalog => catalog
                .Rule("VerteNormaPL", rule => rule.When(ModelVerte).Set("Norma", Option("PL"))))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Option("PL"), configuration.ValueOf("Norma"));
        Assert.Equal(ValueSource.Forced, configuration.SourceOf("Norma"));
        Assert.Contains("Norma", configuration.LockedFeatures);
        Assert.Throws<InvalidOperationException>(() => configuration.Set("Norma", Value.Option("CZ")));
    }

    [Fact]
    public void Forced_value_replaces_the_value_from_the_user()
    {
        var configuration = Start(catalog => catalog
                .Rule("VerteNormaPL", rule => rule.When(ModelVerte).Set("Norma", Option("PL"))))
            .Set("Model", Value.Option("PORTA"))
            .Set("Norma", Value.Option("CZ"))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Option("PL"), configuration.ValueOf("Norma"));
        Assert.Equal(ValueSource.Forced, configuration.SourceOf("Norma"));
    }

    // Layers: default < user < forced; the source names the layer that gave the value.
    [Fact]
    public void Forced_value_wins_over_a_default()
    {
        var configuration = Start(catalog => catalog
                .Rule("NormaPL", rule => rule.Default("Norma", Option("PL")))
                .Rule("VerteNormaCZ", rule => rule.When(ModelVerte).Set("Norma", Option("CZ"))))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Option("CZ"), configuration.ValueOf("Norma"));
        Assert.Equal(ValueSource.Forced, configuration.SourceOf("Norma"));
    }

    private static Configuration StartWithDimensionsTable()
    {
        var wymiary = TableBuilder.Create(["Model", "Norma"], ["SzerokoscMM", "WysokoscMM"])
            .Row([Value.Option("PORTA"), Value.Option("PL")], [Value.Number(844m), Value.Number(2000m)])
            .Row([Value.Option("PORTA"), Value.Option("CZ")], [Value.Number(800m), Value.Number(1970m)])
            .Build();

        return Start(catalog => catalog
            .Table("WymiaryDomyslne", wymiary)
            .Rule("Wymiary", rule => rule.DefaultsFrom(["SzerokoscMM", "WysokoscMM"], "WymiaryDomyslne")));
    }
}
