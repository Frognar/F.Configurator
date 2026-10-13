using static F.Configurator.Engine.Tests.IndexPatterns;

namespace F.Configurator.Engine.Tests;

// Grammar 4.9 and analysis 2.9: the pattern of the collection builds the index from the symbols of the chosen options
// and the numbers of the configuration. Analysis 2.7: only a complete configuration has an index.
public class IndexEncodingTests
{
    private static readonly Expression ModelSymbol = SymbolOf("Model", ("PORTA", "P"), ("VERTE", "V"));
    private static readonly Expression NormaSymbol = SymbolOf("Norma", ("PL", "PL"), ("CZ", "CZ"));

    private static Configuration Start(Expression? pattern)
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature
                .Option("PORTA", "Porta", option => option.Symbol("P"))
                .Option("VERTE", "Verte", option => option.Symbol("V")))
            .Choice("Norma", feature => feature.Option("PL", "Polska").Option("CZ", "Czeska"))
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Rule("SzerokoscDomyslna", rule => rule.Default("SzerokoscMM", Number(844m)))
            .Collection("Basic", collection =>
            {
                var stages = collection.Stage("Model", "Model").Stage("Wymiary", "Norma", "SzerokoscMM");
                return pattern is null ? stages : stages.Index(pattern);
            })
            .Build();

        return Configuration.Start(catalog, "Basic");
    }

    [Fact]
    public void Index_joins_the_symbols_of_the_chosen_options()
    {
        var configuration = Start(Add(ModelSymbol, NormaSymbol))
            .Set("Model", Value.Option("PORTA"))
            .Set("Norma", Value.Option("CZ"));

        Assert.Equal("PCZ", configuration.Index);
    }

    // The pattern can be read before the end, but the product is not known until every feature has a value.
    [Fact]
    public void Incomplete_configuration_has_no_index()
    {
        var configuration = Start(ModelSymbol).Set("Model", Value.Option("PORTA"));

        Assert.Null(configuration.Index);
    }

    [Fact]
    public void Collection_without_a_pattern_has_no_index()
    {
        var configuration = Start(null)
            .Set("Model", Value.Option("PORTA"))
            .Set("Norma", Value.Option("CZ"));

        Assert.Null(configuration.Index);
    }

    [Fact]
    public void Result_has_the_index()
    {
        var result = Start(Add(ModelSymbol, NormaSymbol))
            .Set("Model", Value.Option("VERTE"))
            .Set("Norma", Value.Option("PL"))
            .Result;

        Assert.Equal("VPL", result.Index);
    }
}
