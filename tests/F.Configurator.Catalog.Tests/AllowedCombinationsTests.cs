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

    // Basic: 10 of 32 tables have three or more key columns (kolor-zawiasu has seven).
    [Fact]
    public void Lookup_works_with_three_key_columns()
    {
        var table = CatalogBuilder.Create("Drzwi")
            .AllowedCombinations("DozwoloneUchwyt", t => t
                .Key("Model", "Dwuskrzydlowe", "ZamekDolny")
                .Allowed("Uchwyt")
                .Row("PORTA", "NIE", "KLUCZ", "KLAMKA")
                .Row("PORTA", "TAK", "KLUCZ", "GALKA"))
            .Build()
            .AllowedCombinations["DozwoloneUchwyt"];

        var allowed = table.AllowedFor([Value.Option("PORTA"), Value.Option("NIE"), Value.Option("KLUCZ")]);

        Assert.Equal([Value.Option("KLAMKA")], allowed);
    }

    // Basic: DozwoloneModel (model.csv) has no key column, every row is just an allowed option.
    [Fact]
    public void Table_without_key_columns_allows_its_rows_unconditionally()
    {
        var table = CatalogBuilder.Create("Drzwi")
            .AllowedCombinations("DozwoloneModel", t => t
                .Allowed("Model")
                .Row("PORTA")
                .Row("VERTE"))
            .Build()
            .AllowedCombinations["DozwoloneModel"];

        var allowed = table.AllowedFor([]);

        Assert.Equal([Value.Option("PORTA"), Value.Option("VERTE")], allowed);
    }

    // Key values come from the configuration and may be any value, e.g. a number for Szerokosc
    // (a key column in 5 Basic tables). An unknown key allows nothing; it must not throw.
    [Fact]
    public void Non_option_key_value_finds_no_row_instead_of_throwing()
    {
        var table = CatalogWithUchwyt().AllowedCombinations["DozwoloneUchwyt"];

        var allowed = table.AllowedFor([Value.Number(80), Value.Text("KLUCZ")]);

        Assert.NotNull(allowed);
        Assert.Empty(allowed);
    }

    // Each row is the key cells followed by one allowed option; any other cell count is a typo in
    // code that builds the catalog.
    [Theory]
    [InlineData("PORTA")]
    [InlineData("PORTA", "PELNE", "SZYBA")]
    public void Row_with_a_wrong_number_of_cells_is_rejected(string cell, params string[] cells)
    {
        var builder = AllowedCombinationsTableBuilder.Create().Key("Model").Allowed("Typ");

        Assert.Throws<ArgumentException>(() => builder.Row(cell, cells));
    }

    // Basic keys some tables by `Szerokosc`, a number feature. The engine looks such a table up with
    // NumberValue, so cells must keep their real value type instead of always becoming options.
    [Fact]
    public void Number_key_cells_match_number_values()
    {
        var table = CatalogBuilder.Create("Drzwi")
            .AllowedCombinations("DozwoloneModel", t => t
                .Key("Szerokosc")
                .Allowed("Model")
                .Row(Value.Number(80m), Value.Option("PORTA"))
                .Row(Value.Number(90m), Value.Option("VERTE")))
            .Build()
            .AllowedCombinations["DozwoloneModel"];

        var allowed = table.AllowedFor([Value.Number(80m)]);

        Assert.Equal([Value.Option("PORTA")], allowed);
    }

    [Fact]
    public void Row_of_values_with_a_wrong_number_of_cells_is_rejected()
    {
        var builder = AllowedCombinationsTableBuilder.Create().Key("Szerokosc").Allowed("Model");

        Assert.Throws<ArgumentException>(() => builder.Row(Value.Number(80m)));
    }

    [Fact]
    public void Allowed_combinations_tables_built_the_same_way_are_equal()
    {
        var first = CatalogWithUchwyt().AllowedCombinations["DozwoloneUchwyt"];
        var second = CatalogWithUchwyt().AllowedCombinations["DozwoloneUchwyt"];

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
