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
            .Choice("ZamekDolny", feature => feature.Option("KLUCZ", "Na klucz", symbol: "K"))
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
}
