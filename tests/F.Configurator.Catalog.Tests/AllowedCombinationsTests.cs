using F.Configurator.Expressions;

namespace F.Configurator.Catalog.Tests;

// Grammar G18 (analysis/basic/NOTATKA.md, 5): `tabela T klucz A, B dozwolone C` lists allowed options
// of C for each combination of key values; one key may have many rows. Basic has ~257k rows in 32
// tables, so the lookup goes by key, not by scanning rows.
public class AllowedCombinationsTests
{
    private static Catalog CatalogWithUchwyt() =>
        CatalogBuilder.Create("Drzwi")
            .AllowedCombinations("DozwoloneUchwyt", table => table
                .Key("Model", "ZamekDolny")
                .Allowed("Uchwyt")
                .Row("PORTA", "KLUCZ", "KLAMKA")
                .Row("PORTA", "KLUCZ", "GALKA")
                .Row("PORTA", "BRAK", "GALKA")
                .Row("VERTE", "KLUCZ", "KLAMKA"))
            .Build();

    [Fact]
    public void Builder_keeps_an_allowed_combinations_table()
    {
        var table = CatalogWithUchwyt().AllowedCombinations["DozwoloneUchwyt"];

        Assert.Equal(["Model", "ZamekDolny"], table.KeyColumns);
        Assert.Equal("Uchwyt", table.AllowedFeature);
    }

    // G18.1: allowed options are the values of C in all rows matching the key.
    [Fact]
    public void Allowed_options_come_from_every_row_with_the_key()
    {
        var table = CatalogWithUchwyt().AllowedCombinations["DozwoloneUchwyt"];

        var allowed = table.AllowedFor([Value.Option("PORTA"), Value.Option("KLUCZ")]);

        Assert.Equal([Value.Option("KLAMKA"), Value.Option("GALKA")], allowed);
    }

    // G18.3: no row for the key means the feature does not apply (the engine hides it).
    [Fact]
    public void No_row_for_the_key_allows_nothing()
    {
        var table = CatalogWithUchwyt().AllowedCombinations["DozwoloneUchwyt"];

        var allowed = table.AllowedFor([Value.Option("VERTE"), Value.Option("BRAK")]);

        Assert.NotNull(allowed);
        Assert.Empty(allowed);
    }

    // G18.2: while a key feature has no value the constraint waits; null means "not decided yet",
    // which is different from "nothing allowed".
    [Fact]
    public void Missing_key_value_leaves_the_constraint_undecided()
    {
        var table = CatalogWithUchwyt().AllowedCombinations["DozwoloneUchwyt"];

        var allowed = table.AllowedFor([Value.Option("PORTA"), Value.Missing]);

        Assert.Null(allowed);
    }

    [Fact]
    public void Catalog_keeps_several_allowed_combinations_tables()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .AllowedCombinations("DozwoloneModel", table => table.Key("Kolekcja").Allowed("Model").Row("Basic", "PORTA"))
            .AllowedCombinations("DozwoloneTyp", table => table.Key("Model").Allowed("Typ").Row("PORTA", "PELNE"))
            .Build();

        Assert.Equal(2, catalog.AllowedCombinations.Count);
        Assert.Equal("Typ", catalog.AllowedCombinations["DozwoloneTyp"].AllowedFeature);
    }
}
