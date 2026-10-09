using F.Configurator.Expressions;

namespace F.Configurator.Catalog.Tests;

// Grammar 4.5: a collection groups catalog features into stages; the order of stages and of
// features inside them is the order the configurator asks in sequential mode.
public class CollectionBuilderTests
{
    [Fact]
    public void Builder_creates_a_collection_with_its_stages()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Collection("Basic", collection => collection
                .Stage("Model", "Model", "Typ")
                .Stage("Wymiary", "Norma", "SzerokoscMM"))
            .Build();

        var basic = Assert.Single(catalog.Collections);
        Assert.Equal("Basic", basic.Name);
        Assert.Equal(["Model", "Wymiary"], basic.Stages.Select(stage => stage.Name));
        Assert.Equal(["Norma", "SzerokoscMM"], basic.Stages[1].Features);
    }

    [Fact]
    public void Collection_lists_its_features_in_stage_order()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Collection("Basic", collection => collection
                .Stage("Model", "Model", "Typ")
                .Stage("Wymiary", "Norma", "SzerokoscMM"))
            .Build();

        Assert.Equal(["Model", "Typ", "Norma", "SzerokoscMM"], Assert.Single(catalog.Collections).Features);
    }

    // Grammar 4.5: `tryb kolejny` is the default.
    [Fact]
    public void Collection_is_sequential_by_default()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Collection("Basic", collection => collection.Stage("Model", "Model"))
            .Build();

        Assert.Equal(CollectionMode.Sequential, Assert.Single(catalog.Collections).Mode);
    }

    // Grammar 4.5: `tryb niezależny` lets the user fill features in any order.
    [Fact]
    public void Collection_can_be_independent()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Collection("Basic", collection => collection.Independent().Stage("Model", "Model"))
            .Build();

        Assert.Equal(CollectionMode.Independent, Assert.Single(catalog.Collections).Mode);
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

    // Grammar 4.9: `indeks` is written in the collection block as one expression;
    // turning options into their symbols is the engine's job.
    [Fact]
    public void Collection_keeps_its_index_pattern()
    {
        var pattern = Expression.Add(Expression.Reference("Model"), Expression.Reference("Typ"));

        var catalog = CatalogBuilder.Create("Drzwi")
            .Collection("Basic", collection => collection.Stage("Model", "Model", "Typ").Index(pattern))
            .Build();

        Assert.Same(pattern, Assert.Single(catalog.Collections).Index);
    }

    [Fact]
    public void Collection_without_an_index_pattern_has_none()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Collection("Basic", collection => collection.Stage("Model", "Model"))
            .Build();

        Assert.Null(Assert.Single(catalog.Collections).Index);
    }
}
