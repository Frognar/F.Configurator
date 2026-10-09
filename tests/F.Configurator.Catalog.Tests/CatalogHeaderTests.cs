namespace F.Configurator.Catalog.Tests;

// Grammar 4.1: `katalog "…"`, `wersja "2026.10.1"`, `reakcja błąd` (the reaction of rules without
// `przy naruszeniu`).
public class CatalogHeaderTests
{
    [Fact]
    public void Catalog_keeps_its_version()
    {
        var catalog = CatalogBuilder.Create("Drzwi wewnętrzne")
            .Version("2026.10.1")
            .Build();

        Assert.Equal("2026.10.1", catalog.Version);
    }

    [Fact]
    public void Catalog_keeps_its_default_reaction()
    {
        var catalog = CatalogBuilder.Create("Drzwi wewnętrzne")
            .DefaultReaction(Reaction.Error)
            .Build();

        Assert.Equal(Reaction.Error, catalog.DefaultReaction);
    }
}
