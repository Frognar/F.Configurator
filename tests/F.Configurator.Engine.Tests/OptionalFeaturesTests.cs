using static F.Configurator.Engine.Tests.IndexPatterns;

namespace F.Configurator.Engine.Tests;

// Grammar 4.2: `wymagana NIE`. In Basic the BP edges and the position shifts may stay empty: they do not hold their
// stage, the configuration can be finished without them, and the index writes them only when they are filled in.
public class OptionalFeaturesTests
{
    private static readonly Expression Pattern = Add(
        Add(SymbolOf("Model", ("PORTA", "P"), ("VERTE", "V")), Reference("SzerokoscMM")),
        If(
            HasValue(Reference("ZmianaPolozeniaZawiasu")),
            Add(Text("ZW"), Reference("ZmianaPolozeniaZawiasu")),
            Text("")));

    private static readonly ProductCatalog Catalog = CatalogBuilder.Create("Drzwi")
        .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
        .Choice("KrawedzBP", feature => feature.Option("TAK", "Tak").Option("NIE", "Nie").Optional())
        .Number("SzerokoscMM", feature => feature.Unit("mm"))
        .Number("ZmianaPolozeniaZawiasu", feature => feature.Unit("mm").Optional())
        .Collection("Basic", collection => collection
            .Stage("Wykonanie", "Model", "KrawedzBP")
            .Stage("Wymiary", "SzerokoscMM", "ZmianaPolozeniaZawiasu")
            .Index(Pattern))
        .Build();

    private static Configuration Start() => Configuration.Start(Catalog, "Basic");

    [Fact]
    public void Unset_optional_feature_does_not_hold_its_stage()
    {
        var configuration = Start().Set("Model", Value.Option("PORTA"));

        Assert.Equal("Wymiary", configuration.CurrentStage);
    }

    // The user may still fill in an optional feature of a stage already passed.
    [Fact]
    public void Optional_feature_of_a_passed_stage_can_still_be_set()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(900m))
            .Set("KrawedzBP", Value.Option("TAK"));

        Assert.Equal(Value.Option("TAK"), configuration.ValueOf("KrawedzBP"));
    }

    [Fact]
    public void Configuration_with_unset_optional_features_can_be_finished()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(900m));

        Assert.True(configuration.CanFinish);
        Assert.True(configuration.Result.IsComplete);
    }

    [Fact]
    public void Index_leaves_out_an_unset_optional_feature()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(900m));

        Assert.Equal("P900", configuration.Index);
    }

    [Fact]
    public void Index_writes_a_filled_in_optional_feature()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(900m))
            .Set("ZmianaPolozeniaZawiasu", Value.Number(21m));

        Assert.Equal("P900ZW21", configuration.Index);
    }

    [Fact]
    public void Index_without_an_optional_segment_decodes_with_the_feature_unset()
    {
        var decoded = IndexDecoder.Decode(Catalog, "Basic", "P900");

        Assert.Equal(
            [(Value.Option("PORTA"), Value.Number(900m), Value.Missing)],
            decoded.Select(c => (c.ValueOf("Model"), c.ValueOf("SzerokoscMM"), c.ValueOf("ZmianaPolozeniaZawiasu"))));
    }

    [Fact]
    public void Index_with_an_optional_segment_decodes_with_the_feature_set()
    {
        var decoded = IndexDecoder.Decode(Catalog, "Basic", "P900ZW21");

        Assert.Equal(
            [(Value.Option("PORTA"), Value.Number(900m), Value.Number(21m))],
            decoded.Select(c => (c.ValueOf("Model"), c.ValueOf("SzerokoscMM"), c.ValueOf("ZmianaPolozeniaZawiasu"))));
    }
}
