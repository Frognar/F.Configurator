namespace F.Configurator.Engine.Tests;

// Grammar 6.3: `domyślna(Cecha)` is the value the feature gets by default, also after the user changed it.
// Basic uses it for deviations (`OdchylkaSzerokosc = SzerokoscMM - domyślna(SzerokoscMM)`), for `Nietyp` and for
// limits around the default size.
public class DefaultOfTests
{
    private static Configuration Start(Func<CatalogBuilder, CatalogBuilder> addRules)
    {
        var catalog = addRules(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Number("SzerokoscMM", feature => feature.Unit("mm"))
                .Computed("OdchylkaSzerokosc", Subtract(Reference("SzerokoscMM"), DefaultOf("SzerokoscMM")))
                .Collection("Basic", collection => collection
                    .Stage("Model", "Model")
                    .Stage("Wymiary", "SzerokoscMM")))
            .Build();

        return Configuration.Start(catalog, "Basic").Set("Model", Value.Option("PORTA"));
    }

    private static CatalogBuilder DefaultWidth(CatalogBuilder catalog) =>
        catalog.Rule("SzerokoscDomyslna", rule => rule.Default("SzerokoscMM", Number(944m)));

    [Fact]
    public void Default_of_a_feature_set_by_the_user_is_still_its_default()
    {
        var configuration = Start(DefaultWidth).Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(Value.Number(156m), configuration.ValueOf("OdchylkaSzerokosc"));
    }

    [Fact]
    public void Default_of_a_feature_that_has_its_default_is_that_value()
    {
        var configuration = Start(DefaultWidth);

        Assert.Equal(Value.Number(0m), configuration.ValueOf("OdchylkaSzerokosc"));
    }

    [Fact]
    public void Default_of_a_feature_without_a_default_is_missing()
    {
        var configuration = Start(catalog => catalog).Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(Value.Missing, configuration.ValueOf("OdchylkaSzerokosc"));
    }

    // The default follows the values it depends on, also while the user's value stays.
    [Fact]
    public void Default_of_a_feature_follows_its_rules()
    {
        var configuration = Start(catalog => DefaultWidth(catalog)
                .Rule("SzerokoscVerte", rule => rule
                    .When(Equal(Reference("Model"), Option("VERTE")))
                    .Priority(10)
                    .Default("SzerokoscMM", Number(1044m))))
            .Set("SzerokoscMM", Value.Number(1100m))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Number(56m), configuration.ValueOf("OdchylkaSzerokosc"));
    }

    // Grammar 4.7: `maks SzerokoscMM = domyślna(SzerokoscMM) + 50`.
    [Fact]
    public void Limit_uses_the_default_of_the_feature()
    {
        var configuration = Start(catalog => DefaultWidth(catalog)
                .Rule("MaxSzer", rule => rule
                    .Max("SzerokoscMM", Add(DefaultOf("SzerokoscMM"), Number(50m)))
                    .OnViolation(Reaction.Correct, "Maksymalnie 50 mm więcej niż domyślnie.")))
            .Set("SzerokoscMM", Value.Number(1100m));

        Assert.Equal(Value.Number(994m), configuration.ValueOf("SzerokoscMM"));
    }

    // Tied defaults matter only when they give the feature its value (PriorityTests); `domyślna(…)` of such a
    // feature has no single default, so it is missing.
    [Fact]
    public void Default_of_a_feature_with_tied_defaults_set_by_the_user_is_missing()
    {
        var configuration = Start(catalog => DefaultWidth(catalog)
                .Rule("SzerokoscVerte", rule => rule
                    .When(Equal(Reference("Model"), Option("VERTE")))
                    .Default("SzerokoscMM", Number(1044m))))
            .Set("SzerokoscMM", Value.Number(1100m))
            .Set("Model", Value.Option("VERTE"));

        Assert.Equal(Value.Missing, configuration.ValueOf("OdchylkaSzerokosc"));
    }
}
