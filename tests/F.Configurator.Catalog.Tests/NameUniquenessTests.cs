namespace F.Configurator.Catalog.Tests;

// The builder does not validate catalogs (that is the validator's job), but names other elements refer
// to (features, tables, collections, contexts) must be unique: a repeated name is a programmer error in
// code that builds the catalog, reported with the name. Rules are left to the validator.
public class NameUniquenessTests
{
    private static readonly Table Wymiary = new(
        ["Norma"],
        [new TableRow([[Value.Option("PL")]], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) }.ToEquatableDictionary())]);

    [Fact]
    public void Builder_rejects_a_second_feature_with_the_same_name()
    {
        var builder = CatalogBuilder.Create("Drzwi").Text("Uwagi");

        var error = Assert.Throws<ArgumentException>(() => builder.Boolean("Uwagi"));
        Assert.Contains("'Uwagi'", error.Message);
    }

    [Fact]
    public void Builder_rejects_a_second_table_with_the_same_name()
    {
        var builder = CatalogBuilder.Create("Drzwi").Table("Wymiary", Wymiary);

        var error = Assert.Throws<ArgumentException>(() => builder.Table("Wymiary", Wymiary));
        Assert.Contains("'Wymiary'", error.Message);
    }

    [Fact]
    public void Builder_rejects_a_second_allowed_combinations_table_with_the_same_name()
    {
        var builder = CatalogBuilder.Create("Drzwi")
            .AllowedCombinations("DozwoloneModel", [], "Model", table => table.Row("PORTA"));

        var error = Assert.Throws<ArgumentException>(() =>
            builder.AllowedCombinations("DozwoloneModel", [], "Model", table => table.Row("VERTE")));
        Assert.Contains("'DozwoloneModel'", error.Message);
    }

    // In the DSL both kinds are `tabela`, and effects such as `tylko X z T` refer to them by name only.
    [Fact]
    public void Value_table_and_allowed_combinations_table_share_one_namespace()
    {
        var builder = CatalogBuilder.Create("Drzwi").Table("Wymiary", Wymiary);

        var error = Assert.Throws<ArgumentException>(() =>
            builder.AllowedCombinations("Wymiary", [], "Model", table => table.Row("PORTA")));
        Assert.Contains("'Wymiary'", error.Message);
    }

    [Fact]
    public void Allowed_combinations_table_and_value_table_share_one_namespace()
    {
        var builder = CatalogBuilder.Create("Drzwi")
            .AllowedCombinations("Wymiary", [], "Model", table => table.Row("PORTA"));

        var error = Assert.Throws<ArgumentException>(() => builder.Table("Wymiary", Wymiary));
        Assert.Contains("'Wymiary'", error.Message);
    }

    [Fact]
    public void Builder_rejects_a_second_collection_with_the_same_name()
    {
        var builder = CatalogBuilder.Create("Drzwi").Collection("Basic", c => c.Stage("Model", "Model"));

        var error = Assert.Throws<ArgumentException>(() => builder.Collection("Basic", c => c.Stage("Model", "Model")));
        Assert.Contains("'Basic'", error.Message);
    }

    [Fact]
    public void Builder_rejects_a_second_context_with_the_same_name()
    {
        var builder = CatalogBuilder.Create("Drzwi").Context("Kontrahent", context => context);

        var error = Assert.Throws<ArgumentException>(() => builder.Context("Kontrahent", context => context));
        Assert.Contains("'Kontrahent'", error.Message);
    }
}
