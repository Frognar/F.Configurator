namespace F.Configurator.Catalog.Tests;

// Grammar 4.5: a collection groups catalog features into stages; the order of stages and of
// features inside them is the order the configurator asks in sequential mode.
public class CollectionTests
{
    [Fact]
    public void Collection_keeps_its_stages_in_order()
    {
        var basic = CollectionBuilder.Create("Basic")
            .Stage("Model", "Model", "Typ")
            .Stage("Wymiary", "Norma", "SzerokoscMM")
            .Build();

        Assert.Equal("Basic", basic.Name);
        Assert.Equal([new Stage("Model", ["Model", "Typ"]), new Stage("Wymiary", ["Norma", "SzerokoscMM"])], basic.Stages);
    }

    [Fact]
    public void Collection_lists_its_features_in_stage_order()
    {
        var basic = CollectionBuilder.Create("Basic")
            .Stage("Model", "Model", "Typ")
            .Stage("Wymiary", "Norma", "SzerokoscMM")
            .Build();

        Assert.Equal(["Model", "Typ", "Norma", "SzerokoscMM"], basic.Features);
    }

    // Grammar 4.5: `tryb kolejny` is the default.
    [Fact]
    public void Collection_is_sequential_by_default()
    {
        var basic = CollectionBuilder.Create("Basic").Stage("Model", "Model").Build();

        Assert.Equal(CollectionMode.Sequential, basic.Mode);
    }

    // Grammar 4.5: `tryb niezależny` lets the user fill features in any order.
    [Fact]
    public void Collection_can_be_independent()
    {
        var basic = CollectionBuilder.Create("Basic").Independent().Stage("Model", "Model").Build();

        Assert.Equal(CollectionMode.Independent, basic.Mode);
    }

    // Grammar 4.9: `indeks` is written in the collection block as one expression;
    // turning options into their symbols is the engine's job.
    [Fact]
    public void Collection_keeps_its_index_pattern()
    {
        var pattern = Add(Reference("Model"), Reference("Typ"));

        var basic = CollectionBuilder.Create("Basic").Stage("Model", "Model", "Typ").Index(pattern).Build();

        Assert.Same(pattern, basic.Index);
    }

    [Fact]
    public void Collection_without_an_index_pattern_has_none()
    {
        var basic = CollectionBuilder.Create("Basic").Stage("Model", "Model").Build();

        Assert.Null(basic.Index);
    }

    [Fact]
    public void Catalog_keeps_collections_in_declaration_order()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Collection("Basic", collection => collection.Stage("Model", "Model"))
            .Collection("Brilliant", collection => collection.Stage("Model", "Model"))
            .Build();

        Assert.Equal(["Basic", "Brilliant"], catalog.Collections.Select(collection => collection.Name));
    }
}
