namespace F.Configurator.Engine.Tests;

// Grammar 4.7: `błąd "…"`, `ostrzeż "…"`, `informuj "…"` show a message while the rule's condition holds.
// Analysis 2.5: an error blocks finishing, a warning and an information do not.
public class RuleMessagesTests
{
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(Func<CatalogBuilder, CatalogBuilder> addRules)
    {
        var catalog = addRules(CatalogBuilder.Create("Drzwi")
                .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
                .Collection("Basic", collection => collection.Stage("Model", "Model")))
            .Build();

        return Configuration.Start(catalog, "Basic");
    }

    [Fact]
    public void Rule_message_shows_while_its_condition_holds()
    {
        var start = Start(catalog => catalog.Rule("VerteInfo", rule => rule
            .When(ModelVerte)
            .Inform("Verte ma ościeżnicę regulowaną.")));

        Assert.Equal(
            [new RuleMessage("VerteInfo", Severity.Information, "Verte ma ościeżnicę regulowaną.")],
            start.Set("Model", Value.Option("VERTE")).Messages);
        Assert.Empty(start.Set("Model", Value.Option("PORTA")).Messages);
    }

    [Fact]
    public void Error_message_blocks_finishing()
    {
        var configuration = Start(catalog => catalog.Rule("VerteWycofane", rule => rule
                .When(ModelVerte)
                .Error("Verte jest wycofane z oferty.")))
            .Set("Model", Value.Option("VERTE"));

        Assert.False(configuration.CanFinish);
    }

    [Fact]
    public void Warning_message_allows_finishing()
    {
        var configuration = Start(catalog => catalog.Rule("VerteDlugo", rule => rule
                .When(ModelVerte)
                .Warn("Verte ma długi czas realizacji.")))
            .Set("Model", Value.Option("VERTE"));

        Assert.True(configuration.CanFinish);
    }
}
