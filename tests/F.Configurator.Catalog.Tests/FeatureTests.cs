using F.Configurator.Expressions;

namespace F.Configurator.Catalog.Tests;

// Grammar 4.2: a catalog declares features; a choice feature lists its options.
public class FeatureTests
{
    // Grammar 4.2: `opcja L "Lewe"`; without `symbol` the option's index symbol is its id.
    [Fact]
    public void Choice_feature_keeps_its_options_with_their_names()
    {
        var kierunek = ChoiceFeatureBuilder.Create("Kierunek").Option("L", "Lewe").Option("P", "Prawe").Build();

        Assert.Equal("Kierunek", kierunek.Name);
        Assert.Equal(
            [
                new FeatureOption("L", "Lewe", "L", EquatableDictionary<string, Value>.Empty),
                new FeatureOption("P", "Prawe", "P", EquatableDictionary<string, Value>.Empty),
            ],
            kierunek.Options);
    }

    // Grammar 4.2: `opcja KLUCZ "Na klucz"` with `symbol "K"` puts K in the index instead of the id.
    [Fact]
    public void Option_uses_the_given_symbol()
    {
        var option = FeatureOptionBuilder.Create("KLUCZ", "Na klucz").Symbol("K").Build();

        Assert.Equal(new FeatureOption("KLUCZ", "Na klucz", "K", EquatableDictionary<string, Value>.Empty), option);
    }

    [Fact]
    public void Choice_feature_sets_up_its_option()
    {
        var zamek = ChoiceFeatureBuilder.Create("ZamekDolny").Option("KLUCZ", "Na klucz", option => option.Symbol("K")).Build();

        Assert.Equal("K", Assert.Single(zamek.Options).Symbol);
    }

    [Fact]
    public void Option_keeps_its_attributes()
    {
        var porta = FeatureOptionBuilder.Create("PORTA", "Porta").Attribute("GruboscMM", Value.Number(40m)).Build();

        Assert.Equal(Value.Number(40m), porta.Attributes["GruboscMM"]);
    }

    // Grammar 4.2: `atrybut Seria : tekst` and `atrybut MaRamiak : tak/nie domyślnie NIE` declare
    // option attributes; the validator checks every option against them.
    [Fact]
    public void Choice_feature_keeps_its_attribute_declarations()
    {
        var model = ChoiceFeatureBuilder.Create("Model")
            .Attribute("Seria", AttributeType.Text)
            .Attribute("MaRamiak", AttributeType.Boolean, Value.Boolean(false))
            .Build();

        Assert.Equal(
            [
                new AttributeDeclaration("Seria", AttributeType.Text, null),
                new AttributeDeclaration("MaRamiak", AttributeType.Boolean, Value.Boolean(false)),
            ],
            model.Attributes);
    }

    // Grammar 4.2: attribute types are `tekst`, `liczba` and `tak/nie`, e.g. a leaf thickness in mm.
    [Fact]
    public void Choice_feature_keeps_a_number_attribute_declaration()
    {
        var model = ChoiceFeatureBuilder.Create("Model").Attribute("GruboscMM", AttributeType.Number, Value.Number(40m)).Build();

        Assert.Equal([new AttributeDeclaration("GruboscMM", AttributeType.Number, Value.Number(40m))], model.Attributes);
    }

    [Fact]
    public void Choice_feature_without_attribute_declarations_has_none()
    {
        var kierunek = ChoiceFeatureBuilder.Create("Kierunek").Option("L", "Lewe").Build();

        Assert.Empty(kierunek.Attributes);
    }

    // Grammar 4.2: `cecha SzerokoscMM : liczba` with `jednostka mm`, `min`, `maks` and `krok`.
    // Bounds that depend on other features are rules.
    [Fact]
    public void Number_feature_keeps_its_unit_bounds_and_step()
    {
        var szerokosc = NumberFeatureBuilder.Create("SzerokoscMM").Unit("mm").Min(600m).Max(1200m).Step(10m).Build();

        Assert.Equal(new NumberFeature("SzerokoscMM", "mm", 600m, 1200m, 10m), szerokosc);
    }

    [Fact]
    public void Number_feature_without_settings_has_none()
    {
        var szerokosc = NumberFeatureBuilder.Create("SzerokoscMM").Build();

        Assert.Equal(new NumberFeature("SzerokoscMM", null, null, null, null), szerokosc);
    }

    [Fact]
    public void Catalog_creates_a_boolean_feature()
    {
        var catalog = CatalogBuilder.Create("Drzwi").Boolean("Prog").Build();

        Assert.Equal([new BooleanFeature("Prog")], catalog.Features);
    }

    [Fact]
    public void Catalog_creates_a_text_feature()
    {
        var catalog = CatalogBuilder.Create("Drzwi").Text("Uwagi").Build();

        Assert.Equal([new TextFeature("Uwagi")], catalog.Features);
    }

    // A computed feature has no input from the user; its value comes from an expression.
    [Fact]
    public void Catalog_creates_a_computed_feature_with_its_expression()
    {
        var formula = Expression.Add(Expression.Reference("SzerokoscMM"), Expression.Number(60m));

        var catalog = CatalogBuilder.Create("Drzwi").Computed("SzerokoscOscieznicyMM", formula).Build();

        Assert.Equal([new ComputedFeature("SzerokoscOscieznicyMM", formula)], catalog.Features);
    }

    [Fact]
    public void Catalog_keeps_features_in_declaration_order()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Kierunek", feature => feature.Option("L", "Lewe"))
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Boolean("Prog")
            .Build();

        Assert.Equal(["Kierunek", "SzerokoscMM", "Prog"], catalog.Features.Select(feature => feature.Name));
    }
}
