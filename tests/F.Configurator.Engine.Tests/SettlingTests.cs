namespace F.Configurator.Engine.Tests;

// Analysis 3.2: after every change the engine recomputes values that depend on others until nothing changes.
// A catalog whose values never stop changing is a catalog error, reported with the features that keep changing.
public class SettlingTests
{
    private static ProductCatalog Catalog(Func<CatalogBuilder, CatalogBuilder> addRules) =>
        addRules(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Number("SzerokoscMM", feature => feature.Unit("mm"))
                .Number("WysokoscMM", feature => feature.Unit("mm"))
                .Number("GruboscMM", feature => feature.Unit("mm"))
                .Collection("Basic", collection => collection
                    .Stage("Model", "Model")
                    .Stage("Wymiary", "SzerokoscMM", "WysokoscMM", "GruboscMM")))
            .Build();

    // Each default depends on the one before, so values settle only after several rounds.
    [Fact]
    public void Chain_of_dependent_defaults_settles()
    {
        var configuration = Configuration.Start(
                Catalog(catalog => catalog
                    .Rule("Grubosc", rule => rule.Default("GruboscMM", Add(Reference("WysokoscMM"), Number(1m))))
                    .Rule("Wysokosc", rule => rule.Default("WysokoscMM", Add(Reference("SzerokoscMM"), Number(1m))))
                    .Rule("Szerokosc", rule => rule.Default("SzerokoscMM", Number(900m)))),
                "Basic");

        Assert.Equal(Value.Number(902m), configuration.ValueOf("GruboscMM"));
    }

    [Fact]
    public void Default_that_keeps_changing_is_a_catalog_error()
    {
        var catalog = Catalog(catalog => catalog.Rule("Szerokosc", rule => rule.Default(
            "SzerokoscMM",
            If(Equal(Reference("SzerokoscMM"), Number(900m)), Number(1000m), Number(900m)))));

        var error = Assert.Throws<InvalidOperationException>(() => Configuration.Start(catalog, "Basic"));
        Assert.Contains("'SzerokoscMM'", error.Message);
    }

    // Two defaults that feed each other never settle either.
    [Fact]
    public void Defaults_that_feed_each_other_are_a_catalog_error()
    {
        var catalog = Catalog(catalog => catalog
            .Rule("Szerokosc", rule => rule.Default(
                "SzerokoscMM",
                If(Equal(Reference("WysokoscMM"), Number(2000m)), Number(1000m), Number(900m))))
            .Rule("Wysokosc", rule => rule.Default(
                "WysokoscMM",
                If(Equal(Reference("SzerokoscMM"), Number(900m)), Number(2000m), Number(2100m)))));

        var error = Assert.Throws<InvalidOperationException>(() => Configuration.Start(catalog, "Basic"));
        Assert.Contains("'SzerokoscMM'", error.Message);
        Assert.Contains("'WysokoscMM'", error.Message);
    }
}
