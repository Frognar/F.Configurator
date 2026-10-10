namespace F.Configurator.Engine.Tests;

// Analysis 2.3 and grammar 4.7: `ukryj` hides features and `zablokuj` locks them for editing while the
// rule's condition holds. A feature is hidden (locked) when any matching rule hides (locks) it. Every
// change re-applies the rules; a hidden feature loses its value and behaves as unset (grammar 6.4).
public class HideAndLockTests
{
    private static readonly Expression ZamekBrak = Equal(Reference("ZamekDolny"), Option("BRAK"));
    private static readonly Expression FelcX = Equal(Reference("FelcSkrzydla"), Option("X"));

    private static Configuration Start(Func<CatalogBuilder, CatalogBuilder> addRules)
    {
        var catalog = addRules(CatalogBuilder.Create("Drzwi")
                .Choice("FelcSkrzydla", feature => feature.Option("Z", "Zwykły").Option("X", "X"))
                .Choice("ZamekDolny", feature => feature.Option("KLUCZ", "Na klucz").Option("BRAK", "Brak"))
                .Choice("ZmianaPolozeniaZamka", feature => feature.Option("TAK", "Tak").Option("NIE", "Nie"))
                .Number("SzerokoscMM", feature => feature.Unit("mm"))
                .Collection("Basic", collection => collection
                    .Stage("Skrzydlo", "FelcSkrzydla", "ZamekDolny", "ZmianaPolozeniaZamka")
                    .Stage("Wymiary", "SzerokoscMM")))
            .Build();

        return Configuration.Start(catalog, "Basic");
    }

    private static Configuration StartWithZamekBrakRule() =>
        Start(catalog => catalog.Rule("ZamekBrak", rule => rule.When(ZamekBrak).Hide("ZmianaPolozeniaZamka")));

    private static Configuration StartWithFelcXRule() =>
        Start(catalog => catalog.Rule("FelcXWymiary", rule => rule.When(FelcX).Lock("SzerokoscMM")));

    [Fact]
    public void Matching_rule_hides_the_feature()
    {
        var configuration = StartWithZamekBrakRule().Set("ZamekDolny", Value.Option("BRAK"));

        Assert.Equal(["FelcSkrzydla", "ZamekDolny"], configuration.VisibleFeatures);
    }

    [Fact]
    public void Rule_that_does_not_match_hides_nothing()
    {
        var configuration = StartWithZamekBrakRule().Set("ZamekDolny", Value.Option("KLUCZ"));

        Assert.Equal(["FelcSkrzydla", "ZamekDolny", "ZmianaPolozeniaZamka"], configuration.VisibleFeatures);
    }

    // Grammar 6.4: a condition on a feature without a value is false.
    [Fact]
    public void Rule_on_a_feature_without_a_value_hides_nothing()
    {
        var configuration = StartWithZamekBrakRule();

        Assert.Equal(["FelcSkrzydla", "ZamekDolny", "ZmianaPolozeniaZamka"], configuration.VisibleFeatures);
    }

    [Fact]
    public void Rule_without_a_condition_always_hides()
    {
        var configuration = Start(catalog => catalog.Rule("BezZmiany", rule => rule.Hide("ZmianaPolozeniaZamka")));

        Assert.Equal(["FelcSkrzydla", "ZamekDolny"], configuration.VisibleFeatures);
    }

    // Grammar 4.7: `dotyczy` limits a rule to the listed collections.
    [Fact]
    public void Rule_for_another_collection_hides_nothing()
    {
        var configuration = Start(catalog => catalog.Rule("BezZmiany", rule => rule
            .AppliesTo("Brilliant")
            .Hide("ZmianaPolozeniaZamka")));

        Assert.Equal(["FelcSkrzydla", "ZamekDolny", "ZmianaPolozeniaZamka"], configuration.VisibleFeatures);
    }

    [Fact]
    public void Any_matching_rule_hides_the_feature()
    {
        var configuration = Start(catalog => catalog
                .Rule("FelcX", rule => rule.When(FelcX).Hide("ZmianaPolozeniaZamka"))
                .Rule("ZamekBrak", rule => rule.When(ZamekBrak).Hide("ZmianaPolozeniaZamka")))
            .Set("FelcSkrzydla", Value.Option("Z"))
            .Set("ZamekDolny", Value.Option("BRAK"));

        Assert.DoesNotContain("ZmianaPolozeniaZamka", configuration.VisibleFeatures);
    }

    [Fact]
    public void Setting_a_hidden_feature_is_rejected()
    {
        var configuration = StartWithZamekBrakRule().Set("ZamekDolny", Value.Option("BRAK"));

        Assert.Throws<InvalidOperationException>(() => configuration.Set("ZmianaPolozeniaZamka", Value.Option("TAK")));
    }

    [Fact]
    public void Hidden_feature_loses_its_value()
    {
        var configuration = StartWithZamekBrakRule()
            .Set("ZamekDolny", Value.Option("KLUCZ"))
            .Set("ZmianaPolozeniaZamka", Value.Option("TAK"))
            .Set("ZamekDolny", Value.Option("BRAK"));

        Assert.Equal(Value.Missing, configuration.ValueOf("ZmianaPolozeniaZamka"));
    }

    // The value is cleared, not remembered: showing the feature again does not bring it back.
    [Fact]
    public void Feature_shown_again_has_no_value()
    {
        var configuration = StartWithZamekBrakRule()
            .Set("ZamekDolny", Value.Option("KLUCZ"))
            .Set("ZmianaPolozeniaZamka", Value.Option("TAK"))
            .Set("ZamekDolny", Value.Option("BRAK"))
            .Set("ZamekDolny", Value.Option("KLUCZ"));

        Assert.Equal(Value.Missing, configuration.ValueOf("ZmianaPolozeniaZamka"));
    }

    // A hidden feature is not asked, so it does not hold up its stage.
    [Fact]
    public void Stage_is_complete_without_its_hidden_features()
    {
        var configuration = StartWithZamekBrakRule()
            .Set("FelcSkrzydla", Value.Option("Z"))
            .Set("ZamekDolny", Value.Option("BRAK"));

        Assert.Equal("Wymiary", configuration.CurrentStage);
    }

    [Fact]
    public void Nothing_is_locked_at_start()
    {
        var configuration = StartWithFelcXRule();

        Assert.Empty(configuration.LockedFeatures);
    }

    // A locked feature stays visible with its value; only editing is blocked.
    [Fact]
    public void Matching_rule_locks_the_feature_and_keeps_its_value()
    {
        var configuration = CompleteSkrzydlo(StartWithFelcXRule())
            .Set("SzerokoscMM", Value.Number(900m))
            .Set("FelcSkrzydla", Value.Option("X"));

        Assert.Equal(["SzerokoscMM"], configuration.LockedFeatures);
        Assert.Contains("SzerokoscMM", configuration.VisibleFeatures);
        Assert.Equal(Value.Number(900m), configuration.ValueOf("SzerokoscMM"));
    }

    [Fact]
    public void Setting_a_locked_feature_is_rejected()
    {
        var configuration = CompleteSkrzydlo(StartWithFelcXRule())
            .Set("SzerokoscMM", Value.Number(900m))
            .Set("FelcSkrzydla", Value.Option("X"));

        Assert.Throws<InvalidOperationException>(() => configuration.Set("SzerokoscMM", Value.Number(1000m)));
    }

    [Fact]
    public void Feature_is_unlocked_when_the_rule_stops_matching()
    {
        var configuration = CompleteSkrzydlo(StartWithFelcXRule())
            .Set("SzerokoscMM", Value.Number(900m))
            .Set("FelcSkrzydla", Value.Option("X"))
            .Set("FelcSkrzydla", Value.Option("Z"))
            .Set("SzerokoscMM", Value.Number(1000m));

        Assert.Empty(configuration.LockedFeatures);
        Assert.Equal(Value.Number(1000m), configuration.ValueOf("SzerokoscMM"));
    }

    // Grammar 6.4: a hidden feature behaves as unset, also in the conditions of other rules.
    [Fact]
    public void Rules_see_a_hidden_feature_as_unset()
    {
        var configuration = Start(catalog => catalog
                .Rule("ZamekBrak", rule => rule.When(ZamekBrak).Hide("ZmianaPolozeniaZamka"))
                .Rule("ZmianaWymiary", rule => rule
                    .When(Equal(Reference("ZmianaPolozeniaZamka"), Option("TAK")))
                    .Lock("SzerokoscMM")))
            .Set("FelcSkrzydla", Value.Option("Z"))
            .Set("ZamekDolny", Value.Option("KLUCZ"))
            .Set("ZmianaPolozeniaZamka", Value.Option("TAK"))
            .Set("ZamekDolny", Value.Option("BRAK"));

        Assert.Empty(configuration.LockedFeatures);
    }

    private static Configuration CompleteSkrzydlo(Configuration configuration) => configuration
        .Set("FelcSkrzydla", Value.Option("Z"))
        .Set("ZamekDolny", Value.Option("KLUCZ"))
        .Set("ZmianaPolozeniaZamka", Value.Option("NIE"));
}
