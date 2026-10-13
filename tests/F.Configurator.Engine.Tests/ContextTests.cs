namespace F.Configurator.Engine.Tests;

// Analysis 2.4 (R5, D15) and grammar 4.6: the platform gives the engine the context (the contractor with its
// attributes, the language). For the engine it is an input like a feature that the user does not choose: rules read
// it, the user cannot set it, and it does not change the index. Decided 2026-10-13: the context is a map of its own,
// read by its own node (`ContextValue`), and the platform gives the attributes themselves, not only the contractor.
public class ContextTests
{
    private static readonly Expression NotPremium = NotEqual(ContextValue("Kontrahent", "Asortyment"), Text("PREMIUM"));

    private static ProductCatalog Catalog(Func<CatalogBuilder, CatalogBuilder>? addRules = null)
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Context("Kontrahent", context => context
                .Attribute("Asortyment", AttributeType.Text)
                .Attribute("Rynek", AttributeType.Text)
                .Attribute("MaksymalnaSzerokosc", AttributeType.Number)
                .Attribute("DlugiNapis", AttributeType.Boolean))
            .Choice("Model", feature => feature
                .Option("PORTA", "Porta", option => option.Symbol("P"))
                .Option("PRESTIGE", "Prestige", option => option.Symbol("PR")))
            .Choice("Norma", feature => feature.Option("PL", "Polska").Option("CZ", "Czeska"))
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Rule("SzerokoscDomyslna", rule => rule.Default("SzerokoscMM", Number(900m)))
            .Text("Napis")
            .Rule("NapisDomyslny", rule => rule.Default("Napis", Text("DRZWI")))
            .Rule("AsortymentPremium", rule => rule.When(NotPremium).Forbid("Model", "PRESTIGE"))
            .Rule("NormaCzeska", rule => rule.When(Equal(ContextValue("Język"), Text("cs"))).Default("Norma", Option("CZ")))
            .Collection("Basic", collection => collection
                .Stage("Model", "Model")
                .Stage("Wykonanie", "Norma", "SzerokoscMM", "Napis")
                .Index(Add(
                    IndexPatterns.SymbolOf("Model", ("PORTA", "P"), ("PRESTIGE", "PR")),
                    IndexPatterns.SymbolOf("Norma", ("PL", "PL"), ("CZ", "CZ")))));

        return (addRules?.Invoke(catalog) ?? catalog).Build();
    }

    private static readonly Expression NormaOfTheMarket =
        If(Equal(ContextValue("Kontrahent", "Rynek"), Text("CZ")), Option("CZ"), Option("PL"));

    private static Dictionary<string, Value> Market(string market) =>
        new() { ["Kontrahent.Rynek"] = Value.Text(market) };

    private static Dictionary<string, Value> Assortment(string assortment) =>
        new() { ["Kontrahent.Asortyment"] = Value.Text(assortment) };

    [Fact]
    public void Rule_reads_an_attribute_of_the_contractor()
    {
        var standard = Configuration.Start(Catalog(), "Basic", Assortment("STANDARD"));
        var premium = Configuration.Start(Catalog(), "Basic", Assortment("PREMIUM"));

        Assert.Equal([new UnavailableOption("PRESTIGE", "AsortymentPremium")], standard.UnavailableOptions("Model"));
        Assert.Empty(premium.UnavailableOptions("Model"));
    }

    [Fact]
    public void Context_stays_with_the_configuration_after_a_change()
    {
        var configuration = Configuration.Start(Catalog(), "Basic", Assortment("PREMIUM"))
            .Set("Model", Value.Option("PORTA"));

        Assert.Equal(["PORTA", "PRESTIGE"], configuration.AvailableOptions("Model"));
    }

    [Fact]
    public void Rule_reads_the_language()
    {
        var configuration = Configuration.Start(
                Catalog(), "Basic", new Dictionary<string, Value> { ["Język"] = Value.Text("cs") })
            .Set("Model", Value.Option("PORTA"));

        Assert.Equal(Value.Option("CZ"), configuration.ValueOf("Norma"));
    }

    // Rules read the context in their effects too, not only in their conditions.
    [Fact]
    public void Default_reads_the_context()
    {
        var configuration = Configuration.Start(
                Catalog(catalog => catalog.Rule("NormaRynku", rule => rule.Default("Norma", NormaOfTheMarket))),
                "Basic",
                Market("CZ"))
            .Set("Model", Value.Option("PORTA"));

        Assert.Equal(Value.Option("CZ"), configuration.ValueOf("Norma"));
    }

    [Fact]
    public void Forced_value_reads_the_context()
    {
        var configuration = Configuration.Start(
                Catalog(catalog => catalog.Rule("NormaRynku", rule => rule.Set("Norma", NormaOfTheMarket))),
                "Basic",
                Market("CZ"))
            .Set("Model", Value.Option("PORTA"));

        Assert.Equal(Value.Option("CZ"), configuration.ValueOf("Norma"));
    }

    [Fact]
    public void Limit_reads_the_context()
    {
        var configuration = Configuration.Start(
            Catalog(catalog => catalog.Rule("SzerokoscKontrahenta", rule => rule
                .Max("SzerokoscMM", ContextValue("Kontrahent", "MaksymalnaSzerokosc")))),
            "Basic",
            new Dictionary<string, Value> { ["Kontrahent.MaksymalnaSzerokosc"] = Value.Number(1000m) });

        Assert.Equal(new NumberLimits(null, 1000m, null), configuration.LimitsOf("SzerokoscMM"));
    }

    [Fact]
    public void Text_length_reads_the_context()
    {
        var configuration = Configuration.Start(
            Catalog(catalog => catalog.Rule("NapisKontrahenta", rule => rule
                .Length("Napis", If(ContextValue("Kontrahent", "DlugiNapis"), Range(1m, 10m), Range(1m, 5m))))),
            "Basic",
            new Dictionary<string, Value> { ["Kontrahent.DlugiNapis"] = Value.Boolean(true) });

        Assert.Equal(new TextLength(1, 10), configuration.LengthOf("Napis"));
    }

    [Fact]
    public void Computed_feature_reads_the_context()
    {
        var configuration = Configuration.Start(
            Catalog(catalog => catalog.Computed(
                "SzerokoscGraniczna", Add(ContextValue("Kontrahent", "MaksymalnaSzerokosc"), Number(60m)))),
            "Basic",
            new Dictionary<string, Value> { ["Kontrahent.MaksymalnaSzerokosc"] = Value.Number(1000m) });

        Assert.Equal(Value.Number(1060m), configuration.ValueOf("SzerokoscGraniczna"));
    }

    // The contractor is given by the server of the platform; otherwise a partner could unlock options of others.
    [Fact]
    public void User_cannot_set_the_context()
    {
        var configuration = Configuration.Start(Catalog(), "Basic", Assortment("STANDARD"));

        Assert.Throws<ArgumentException>(() =>
            configuration.Set("Kontrahent.Asortyment", Value.Text("PREMIUM")));
    }

    // Grammar 4.6: `kontekst Kontrahent` declares the attributes; `Kontrahent` and `Język` need no declaration.
    [Fact]
    public void Undeclared_attribute_of_the_context_is_rejected()
    {
        var error = Assert.Throws<ArgumentException>(() => Configuration.Start(
            Catalog(), "Basic", new Dictionary<string, Value> { ["Kontrahent.Rabat"] = Value.Number(10m) }));

        Assert.Contains("Kontrahent.Rabat", error.Message);
    }

    [Fact]
    public void Built_in_context_values_need_no_declaration()
    {
        var configuration = Configuration.Start(
            Catalog(),
            "Basic",
            new Dictionary<string, Value> { ["Kontrahent"] = Value.Text("13-WEBICO"), ["Język"] = Value.Text("pl") });

        Assert.Equal("Model", configuration.CurrentStage);
    }

    [Fact]
    public void Attribute_of_another_type_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => Configuration.Start(
            Catalog(), "Basic", new Dictionary<string, Value> { ["Kontrahent.Rynek"] = Value.Number(1m) }));
    }

    // Grammar 5: price conditions see only the step result, with the context in it.
    [Fact]
    public void Result_carries_the_context()
    {
        var configuration = Configuration.Start(Catalog(), "Basic", Assortment("PREMIUM"));

        Assert.Equal(
            new Dictionary<string, Value> { ["Kontrahent.Asortyment"] = Value.Text("PREMIUM") }.ToEquatableDictionary(),
            configuration.Result.Context);
    }

    [Fact]
    public void Context_is_not_a_feature_of_the_result()
    {
        var configuration = Configuration.Start(Catalog(), "Basic", Assortment("PREMIUM"));

        Assert.Equal(
            ["Model", "Norma", "SzerokoscMM", "Napis"],
            configuration.Result.Features.Select(feature => feature.Name));
    }

    // The index identifies the product, not the buyer: the same choices give the same index for every contractor.
    [Fact]
    public void Index_does_not_depend_on_the_context()
    {
        var standard = Configuration.Start(Catalog(), "Basic", Assortment("STANDARD"))
            .Set("Model", Value.Option("PORTA"))
            .Set("Norma", Value.Option("PL"));
        var premium = Configuration.Start(Catalog(), "Basic", Assortment("PREMIUM"))
            .Set("Model", Value.Option("PORTA"))
            .Set("Norma", Value.Option("PL"));

        Assert.Equal("PPL", standard.Index);
        Assert.Equal(standard.Index, premium.Index);
    }

    // The same index may stand for a product the contractor cannot order, so decoding takes the context too.
    [Fact]
    public void Index_decodes_within_the_context()
    {
        Assert.Empty(IndexDecoder.Decode(Catalog(), "Basic", "PRPL", Assortment("STANDARD")));
        Assert.Equal(
            [Value.Option("PRESTIGE")],
            IndexDecoder.Decode(Catalog(), "Basic", "PRPL", Assortment("PREMIUM")).Select(c => c.ValueOf("Model")));
    }
}
