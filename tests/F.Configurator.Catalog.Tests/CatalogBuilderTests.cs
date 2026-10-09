using F.Configurator.Expressions;

namespace F.Configurator.Catalog.Tests;

// Grammar 4.2: a catalog declares features; a choice feature lists its options.
public class CatalogBuilderTests
{
    [Fact]
    public void Builder_creates_a_choice_feature_with_its_options()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Kierunek", feature => feature.Option("L", "Lewe").Option("P", "Prawe"))
            .Build();

        var kierunek = Assert.IsType<ChoiceFeature>(Assert.Single(catalog.Features));
        Assert.Equal("Kierunek", kierunek.Name);
        Assert.Equal(["L", "P"], kierunek.Options.Select(option => option.Id));
    }

    // Grammar 4.1: `katalog "Drzwi wewnętrzne"` names the catalog.
    [Fact]
    public void Builder_keeps_the_catalog_name()
    {
        var catalog = CatalogBuilder.Create("Drzwi wewnętrzne").Build();

        Assert.Equal("Drzwi wewnętrzne", catalog.Name);
    }

    // Grammar 4.2: `opcja L "Lewe"` gives the option a display name.
    [Fact]
    public void Option_keeps_its_name()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Kierunek", feature => feature.Option("L", "Lewe"))
            .Build();

        var option = Assert.Single(Assert.IsType<ChoiceFeature>(Assert.Single(catalog.Features)).Options);
        Assert.Equal("Lewe", option.Name);
    }

    // Grammar 4.2: without `symbol` the option's index symbol is its id.
    [Fact]
    public void Option_symbol_defaults_to_its_id()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Kierunek", feature => feature.Option("L", "Lewe"))
            .Build();

        var option = Assert.Single(Assert.IsType<ChoiceFeature>(Assert.Single(catalog.Features)).Options);
        Assert.Equal("L", option.Symbol);
    }

    // Grammar 4.2: `opcja KLUCZ "Na klucz"` with `symbol "K"` puts K in the index instead of the id.
    [Fact]
    public void Option_uses_the_given_symbol()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("ZamekDolny", feature => feature.Option("KLUCZ", "Na klucz", option => option.Symbol("K")))
            .Build();

        var option = Assert.Single(Assert.IsType<ChoiceFeature>(Assert.Single(catalog.Features)).Options);
        Assert.Equal("K", option.Symbol);
    }

    // Grammar 4.2: `cecha SzerokoscMM : liczba` with `jednostka mm`.
    [Fact]
    public void Builder_creates_a_number_feature_with_its_unit()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Build();

        var szerokosc = Assert.IsType<NumberFeature>(Assert.Single(catalog.Features));
        Assert.Equal("SzerokoscMM", szerokosc.Name);
        Assert.Equal("mm", szerokosc.Unit);
    }

    // Grammar 4.2: `krok 1` sets the step of a number feature.
    [Fact]
    public void Number_feature_keeps_its_step()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Number("SzerokoscMM", feature => feature.Step(1m))
            .Build();

        var szerokosc = Assert.IsType<NumberFeature>(Assert.Single(catalog.Features));
        Assert.Equal(1m, szerokosc.Step);
    }

    // Grammar 4.2: `min` and `maks` are fixed bounds; bounds that depend on other features are rules.
    [Fact]
    public void Number_feature_keeps_its_bounds()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Number("SzerokoscMM", feature => feature.Min(600m).Max(1200m))
            .Build();

        var szerokosc = Assert.IsType<NumberFeature>(Assert.Single(catalog.Features));
        Assert.Equal(600m, szerokosc.Min);
        Assert.Equal(1200m, szerokosc.Max);
    }

    [Fact]
    public void Builder_creates_a_boolean_feature()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Boolean("Prog")
            .Build();

        var prog = Assert.IsType<BooleanFeature>(Assert.Single(catalog.Features));
        Assert.Equal("Prog", prog.Name);
    }

    [Fact]
    public void Builder_creates_a_text_feature()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Text("Uwagi")
            .Build();

        var uwagi = Assert.IsType<TextFeature>(Assert.Single(catalog.Features));
        Assert.Equal("Uwagi", uwagi.Name);
    }

    [Fact]
    public void Option_keeps_its_attributes()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature
                .Option("PORTA", "Porta", option => option.Attribute("GruboscMM", new NumberValue(40m))))
            .Build();

        var model = Assert.IsType<ChoiceFeature>(Assert.Single(catalog.Features));
        var porta = Assert.Single(model.Options);
        Assert.Equal(new NumberValue(40m), porta.Attributes["GruboscMM"]);
    }

    // A computed feature has no input from the user; its value comes from an expression.
    [Fact]
    public void Builder_creates_a_computed_feature_with_its_expression()
    {
        var formula = Expression.Add(Expression.Reference("SzerokoscMM"), Expression.Number(60m));

        var catalog = CatalogBuilder.Create("Drzwi")
            .Computed("SzerokoscOscieznicyMM", formula)
            .Build();

        var oscieznica = Assert.IsType<ComputedFeature>(Assert.Single(catalog.Features));
        Assert.Equal("SzerokoscOscieznicyMM", oscieznica.Name);
        Assert.Same(formula, oscieznica.Expression);
    }

    // Grammar 4.4: a catalog declares named tables; expressions read them as `Wymiary[Norma].SzerokoscMM`.
    [Fact]
    public void Builder_keeps_a_named_table()
    {
        var wymiary = new Table(
            ["Norma"],
            [new TableRow([[Value.Option("PL")]], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) })]);

        var catalog = CatalogBuilder.Create("Drzwi")
            .Table("Wymiary", wymiary)
            .Build();

        Assert.Same(wymiary, catalog.Tables["Wymiary"]);
    }

    // The builder does not validate catalogs (that is the validator's job), but two features
    // with one name is a programmer error in code that builds the catalog.
    [Fact]
    public void Builder_rejects_a_second_feature_with_the_same_name()
    {
        var builder = CatalogBuilder.Create("Drzwi").Text("Uwagi");

        Assert.Throws<ArgumentException>(() => builder.Boolean("Uwagi"));
    }

    // Names other elements refer to (features, tables, collections) must be unique; a repeated name
    // is a programmer error, reported with the name. Rules are left to the validator.
    private static readonly Table Wymiary = new(
        ["Norma"],
        [new TableRow([[Value.Option("PL")]], new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m) })]);

    [Fact]
    public void Builder_rejects_a_second_table_with_the_same_name()
    {
        var builder = CatalogBuilder.Create("Drzwi").Table("Wymiary", Wymiary);

        var error = Assert.Throws<ArgumentException>(() => builder.Table("Wymiary", Wymiary));
        Assert.Contains("'Wymiary'", error.Message);
    }

    // In the DSL both kinds are `tabela`, and effects such as `tylko X z T` refer to them by name only.
    [Fact]
    public void Value_table_and_allowed_combinations_table_share_one_namespace()
    {
        var builder = CatalogBuilder.Create("Drzwi").Table("Wymiary", Wymiary);

        var error = Assert.Throws<ArgumentException>(() =>
            builder.AllowedCombinations("Wymiary", table => table.Allowed("Model").Row("PORTA")));
        Assert.Contains("'Wymiary'", error.Message);
    }

    [Fact]
    public void Allowed_combinations_table_and_value_table_share_one_namespace()
    {
        var builder = CatalogBuilder.Create("Drzwi")
            .AllowedCombinations("Wymiary", table => table.Allowed("Model").Row("PORTA"));

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
    public void Builder_rejects_a_second_allowed_combinations_table_with_the_same_name()
    {
        var builder = CatalogBuilder.Create("Drzwi")
            .AllowedCombinations("DozwoloneModel", table => table.Allowed("Model").Row("PORTA"));

        var error = Assert.Throws<ArgumentException>(() =>
            builder.AllowedCombinations("DozwoloneModel", table => table.Allowed("Model").Row("VERTE")));
        Assert.Contains("'DozwoloneModel'", error.Message);
    }
}
