namespace F.Configurator.Engine.Tests;

// Grammar 4.3: `wyliczana Nazwa = wyrażenie`. The engine computes the value from other features; the user cannot
// edit it, but rules, the index and prices use it like any feature.
public class ComputedFeaturesTests
{
    private static readonly Expression FrameWidth = Add(Reference("SzerokoscMM"), Number(60m));

    private static Configuration Start(
        Func<CatalogBuilder, CatalogBuilder>? addRules = null,
        params IEnumerable<string> alsoInSizes)
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Boolean("Prog")
            .Computed("SzerokoscOscieznicyMM", FrameWidth)
            .Collection("Basic", collection => collection
                .Stage("Wymiary", "SzerokoscMM", alsoInSizes)
                .Stage("Dodatki", "Prog"));

        return Configuration.Start((addRules?.Invoke(catalog) ?? catalog).Build(), "Basic");
    }

    [Fact]
    public void Computed_feature_takes_the_value_of_its_expression()
    {
        var configuration = Start().Set("SzerokoscMM", Value.Number(900m));

        Assert.Equal(Value.Number(960m), configuration.ValueOf("SzerokoscOscieznicyMM"));
    }

    [Fact]
    public void Computed_value_comes_from_the_engine()
    {
        var configuration = Start().Set("SzerokoscMM", Value.Number(900m));

        Assert.Equal(ValueSource.Computed, configuration.SourceOf("SzerokoscOscieznicyMM"));
    }

    // Grammar 6.4: arithmetic with a missing value gives a missing value, and a missing value has no source.
    [Fact]
    public void Computed_feature_without_its_inputs_has_no_value()
    {
        var configuration = Start();

        Assert.Equal(Value.Missing, configuration.ValueOf("SzerokoscOscieznicyMM"));
        Assert.Null(configuration.SourceOf("SzerokoscOscieznicyMM"));
    }

    // Analysis 1.7: computed values are used in conditions like features.
    [Fact]
    public void Rule_condition_uses_a_computed_feature()
    {
        var configuration = Start(catalog => catalog.Rule("SzerokaBezProgu", rule => rule
                .When(GreaterThan(Reference("SzerokoscOscieznicyMM"), Number(1000m)))
                .Hide("Prog")))
            .Set("SzerokoscMM", Value.Number(1000m));

        Assert.Equal(["SzerokoscMM"], configuration.VisibleFeatures);
    }

    // A condition sees all values at once: computed values do not replace the others.
    [Fact]
    public void Rule_condition_sees_computed_values_together_with_the_others()
    {
        var configuration = Start(catalog => catalog.Rule("NietypowaBezProgu", rule => rule
                .When(NotEqual(Reference("SzerokoscMM"), Number(900m)))
                .Hide("Prog")))
            .Set("SzerokoscMM", Value.Number(900m));

        Assert.Equal(["SzerokoscMM", "Prog"], configuration.VisibleFeatures);
    }

    // Like `OdchylkaSzerokosc` used by `Nietyp`: a computed feature may use one declared before it.
    [Fact]
    public void Computed_feature_uses_a_computed_feature_declared_before_it()
    {
        var configuration = Start(catalog => catalog
                .Computed("NietypowaOscieznica", If(GreaterThan(Reference("SzerokoscOscieznicyMM"), Number(1000m)), Text("T"), Text("N"))))
            .Set("SzerokoscMM", Value.Number(1000m));

        Assert.Equal(Value.Text("T"), configuration.ValueOf("NietypowaOscieznica"));
    }

    // Grammar 4.3: a computed feature shown in the wizard is read-only.
    [Fact]
    public void Computed_feature_in_a_stage_is_locked()
    {
        var configuration = Start(alsoInSizes: "SzerokoscOscieznicyMM").Set("SzerokoscMM", Value.Number(900m));

        Assert.Contains("SzerokoscOscieznicyMM", configuration.LockedFeatures);
        Assert.Throws<InvalidOperationException>(() => configuration.Set("SzerokoscOscieznicyMM", Value.Number(1000m)));
    }
}
