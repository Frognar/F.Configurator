namespace F.Configurator.Engine.Tests;

// Analysis 3.2, change of a value, step 3: with the new state and the step result the engine returns the list of
// changes it made, e.g. "SzerokośćMM changed from 1100 to 1044, rule MaxSzerBasic". The user's own change is not on
// the list, unless a rule changed what the user entered. Each change names its cause and, when there is one, its rule.
public class ValueChangesTests
{
    private static readonly Expression ModelPorta = Equal(Reference("Model"), Option("PORTA"));
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(Func<CatalogBuilder, CatalogBuilder>? addRules = null)
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Choice("Kolor", feature => feature.Option("BIALY", "Biały").Option("DAB", "Dąb"))
            .Rule("SzerokoscDomyslna", rule => rule.Default("SzerokoscMM", Number(944m)))
            .Collection("Basic", collection => collection
                .Stage("Model", "Model")
                .Stage("Wymiary", "SzerokoscMM", "Kolor"));

        return Configuration.Start((addRules?.Invoke(catalog) ?? catalog).Build(), "Basic");
    }

    [Fact]
    public void Start_has_no_changes()
    {
        Assert.Empty(Start().Changes);
    }

    [Fact]
    public void Change_made_by_the_user_is_not_listed()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(1000m));

        Assert.Empty(configuration.Changes);
    }

    [Fact]
    public void Default_changed_by_a_rule_is_listed()
    {
        var configuration = Start(catalog => catalog.Rule("SzerokoscPorta", rule => rule
                .When(ModelPorta)
                .Priority(1)
                .Default("SzerokoscMM", Number(844m))))
            .Set("Model", Value.Option("PORTA"));

        Assert.Equal(
            [new ValueChange("SzerokoscMM", Value.Number(944m), Value.Number(844m), ChangeCause.Default, "SzerokoscPorta")],
            configuration.Changes);
    }

    // The change starts from the entered value, so the user sees what the rule did to it.
    [Fact]
    public void Entered_value_corrected_by_a_rule_is_listed()
    {
        var configuration = Start(catalog => catalog.Rule("MaxSzerokosc", rule => rule
                .Max("SzerokoscMM", Number(1044m))
                .OnViolation(Reaction.Correct, "Maksymalna szerokość to 1044 mm.")))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(
            [new ValueChange("SzerokoscMM", Value.Number(1100m), Value.Number(1044m), ChangeCause.Corrected, "MaxSzerokosc")],
            configuration.Changes);
    }

    // The correction is the cause, even when a default then fills the cleared choice.
    [Fact]
    public void Choice_cleared_by_a_rule_is_listed_as_corrected()
    {
        var configuration = Start(catalog => catalog
                .Rule("KolorBialy", rule => rule.Default("Kolor", Option("BIALY")))
                .Rule("VerteBezDebu", rule => rule
                    .When(ModelVerte)
                    .Forbid("Kolor", "DAB")
                    .OnViolation(Reaction.Correct, "Verte nie ma koloru dąb.")))
            .Set("Model", Value.Option("PORTA"))
            .Set("Kolor", Value.Option("DAB"))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(
            [new ValueChange("Kolor", Value.Option("DAB"), Value.Option("BIALY"), ChangeCause.Corrected, "VerteBezDebu")],
            configuration.Changes);
    }

    [Fact]
    public void Forced_value_is_listed()
    {
        var configuration = Start(catalog => catalog.Rule("VerteWaska", rule => rule
                .When(ModelVerte)
                .Set("SzerokoscMM", Number(900m))))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(
            [new ValueChange("SzerokoscMM", Value.Number(944m), Value.Number(900m), ChangeCause.Forced, "VerteWaska")],
            configuration.Changes);
    }

    [Fact]
    public void Computed_value_is_listed()
    {
        var configuration = Start(catalog => catalog
                .Computed("SzerokoscOscieznicyMM", Add(Reference("SzerokoscMM"), Number(60m))))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(1000m));

        Assert.Equal(
            [new ValueChange("SzerokoscOscieznicyMM", Value.Number(1004m), Value.Number(1060m), ChangeCause.Computed, null)],
            configuration.Changes);
    }

    [Fact]
    public void Value_of_a_hidden_feature_is_listed()
    {
        var configuration = Start(catalog => catalog.Rule("VerteBezKoloru", rule => rule
                .When(ModelVerte)
                .Hide("Kolor")))
            .Set("Model", Value.Option("PORTA"))
            .Set("Kolor", Value.Option("DAB"))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(
            [new ValueChange("Kolor", Value.Option("DAB"), Value.Missing, ChangeCause.Hidden, "VerteBezKoloru")],
            configuration.Changes);
    }

    // Grammar G18: a table with no row for the key hides the feature.
    [Fact]
    public void Value_of_a_feature_hidden_by_a_table_is_listed()
    {
        var configuration = Start(catalog => catalog
                .AllowedCombinations("DozwoloneKolory", ["Model"], "Kolor", table => table
                    .Row("PORTA", "BIALY")
                    .Row("PORTA", "DAB"))
                .Rule("KolorZTabeli", rule => rule.OnlyFrom("Kolor", "DozwoloneKolory")))
            .Set("Model", Value.Option("PORTA"))
            .Set("Kolor", Value.Option("DAB"))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(
            [new ValueChange("Kolor", Value.Option("DAB"), Value.Missing, ChangeCause.Hidden, "KolorZTabeli")],
            configuration.Changes);
    }

    // The rule that gave the value no longer applies and nothing replaces it.
    [Fact]
    public void Value_whose_rule_no_longer_applies_is_listed()
    {
        var configuration = Start(catalog => catalog.Rule("KolorPorta", rule => rule
                .When(ModelPorta)
                .Default("Kolor", Option("BIALY"))))
            .Set("Model", Value.Option("PORTA"))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(
            [new ValueChange("Kolor", Value.Option("BIALY"), Value.Missing, ChangeCause.Removed, null)],
            configuration.Changes);
    }
}
