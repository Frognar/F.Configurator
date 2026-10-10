namespace F.Configurator.Catalog.Tests;

// Grammar 4.1: `katalog "…"`, `wersja "2026.10.1"`, `reakcja błąd` (the reaction of rules without
// `przy naruszeniu`).
public class CatalogTests
{
    [Fact]
    public void Catalog_keeps_its_name()
    {
        var catalog = CatalogBuilder.Create("Drzwi wewnętrzne").Build();

        Assert.Equal("Drzwi wewnętrzne", catalog.Name);
    }

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

    // Grammar 4.4: a catalog declares named tables; expressions read them as `Wymiary[Norma].SzerokoscMM`.
    [Fact]
    public void Catalog_keeps_a_named_table()
    {
        var wymiary = TableBuilder.Create(["Norma"], ["SzerokoscMM"])
            .Row([Value.Option("PL")], [Value.Number(844m)])
            .Build();

        var catalog = CatalogBuilder.Create("Drzwi")
            .Table("Wymiary", wymiary)
            .Build();

        Assert.Same(wymiary, catalog.Tables["Wymiary"]);
    }
}
