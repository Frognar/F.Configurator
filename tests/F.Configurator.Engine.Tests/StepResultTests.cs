namespace F.Configurator.Engine.Tests;

// Analysis 2.7: after every change the engine gives the step result, the only input of the price layer. It must be
// enough to price a configuration without asking the engine or the catalog: catalog and collection, every feature
// with its value, symbol, source and visibility, computed features, attributes of chosen options, messages and
// completeness.
public class StepResultTests
{
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));

    private static Configuration Start(
        Func<CatalogBuilder, CatalogBuilder>? addRules = null,
        params IEnumerable<string> alsoInSizes)
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Version("2026.10")
            .Choice("Model", feature => feature
                .Attribute("Seria", AttributeType.Text)
                .Option("PORTA", "Porta", option => option.Symbol("P").Attribute("Seria", Value.Text("ASTORIA")))
                .Option("VERTE", "Verte", option => option.Symbol("V").Attribute("Seria", Value.Text("NOVA"))))
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Boolean("Prog")
            .Rule("SzerokoscDomyslna", rule => rule.Default("SzerokoscMM", Number(944m)))
            .Rule("VerteBezProgu", rule => rule.When(ModelVerte).Hide("Prog"))
            .Collection("Basic", collection => collection
                .Stage("Model", "Model")
                .Stage("Wymiary", "SzerokoscMM", ["Prog", .. alsoInSizes]));

        return Configuration.Start((addRules?.Invoke(catalog) ?? catalog).Build(), "Basic");
    }

    private static FeatureResult FeatureOf(StepResult result, string name) =>
        result.Features.Single(feature => feature.Name == name);

    [Fact]
    public void Result_names_the_catalog_its_version_and_the_collection()
    {
        var result = Start().Result;

        Assert.Equal(("Drzwi", "2026.10", "Basic"), (result.Catalog, result.Version, result.Collection));
    }

    // Features come in stage order; a feature without a value is listed too, with a missing value.
    [Fact]
    public void Result_has_the_value_of_every_feature_of_the_collection()
    {
        var result = Start().Set("Model", Value.Option("PORTA")).Result;

        Assert.Equal(
            [("Model", Value.Option("PORTA")), ("SzerokoscMM", Value.Number(944m)), ("Prog", Value.Missing)],
            result.Features.Select(feature => (feature.Name, feature.Value)));
    }

    [Fact]
    public void Result_gives_the_source_of_each_value()
    {
        var result = Start().Set("Model", Value.Option("PORTA")).Result;

        Assert.Equal(
            [("Model", ValueSource.User), ("SzerokoscMM", ValueSource.Default), ("Prog", (ValueSource?)null)],
            result.Features.Select(feature => (feature.Name, feature.Source)));
    }

    [Fact]
    public void Result_tells_which_features_are_visible()
    {
        var result = Start().Set("Model", Value.Option("VERTE")).Result;

        Assert.Equal(
            [("Model", true), ("SzerokoscMM", true), ("Prog", false)],
            result.Features.Select(feature => (feature.Name, feature.IsVisible)));
    }

    // Grammar 4.2: the symbol of an option goes to the index; other values have no symbol.
    [Fact]
    public void Result_gives_the_symbol_of_a_chosen_option()
    {
        var result = Start().Set("Model", Value.Option("VERTE")).Result;

        Assert.Equal(
            [("Model", "V"), ("SzerokoscMM", null), ("Prog", (string?)null)],
            result.Features.Select(feature => (feature.Name, feature.Symbol)));
    }

    // Prices read attributes like `Model.Seria` from the result, not from the catalog.
    [Fact]
    public void Result_gives_the_attributes_of_a_chosen_option()
    {
        var result = Start().Set("Model", Value.Option("PORTA")).Result;

        Assert.Equal(new Dictionary<string, Value> { ["Seria"] = Value.Text("ASTORIA") }, FeatureOf(result, "Model").Attributes);
        Assert.Empty(FeatureOf(result, "SzerokoscMM").Attributes);
    }

    // Computed features are not in the stages, so they come after the features of the collection.
    [Fact]
    public void Result_lists_computed_features_after_the_features_of_the_collection()
    {
        var result = Start(catalog => catalog.Computed("SzerokoscOscieznicyMM", Add(Reference("SzerokoscMM"), Number(60m))))
            .Set("Model", Value.Option("PORTA"))
            .Result;

        Assert.Equal(["Model", "SzerokoscMM", "Prog", "SzerokoscOscieznicyMM"], result.Features.Select(feature => feature.Name));
        Assert.Equal(
            (Value.Number(1004m), (ValueSource?)ValueSource.Computed),
            (FeatureOf(result, "SzerokoscOscieznicyMM").Value, FeatureOf(result, "SzerokoscOscieznicyMM").Source));
    }

    // Grammar 4.3: a computed feature may also be shown in a stage; the result still lists it once.
    [Fact]
    public void Result_lists_a_computed_feature_of_a_stage_once()
    {
        var result = Start(
                catalog => catalog.Computed("SzerokoscOscieznicyMM", Add(Reference("SzerokoscMM"), Number(60m))),
                alsoInSizes: "SzerokoscOscieznicyMM")
            .Result;

        Assert.Equal(["Model", "SzerokoscMM", "Prog", "SzerokoscOscieznicyMM"], result.Features.Select(feature => feature.Name));
    }

    [Fact]
    public void Result_has_the_violations_of_the_step()
    {
        var result = Start(catalog => catalog.Rule("MaxSzerokosc", rule => rule
                .Max("SzerokoscMM", Number(1044m))
                .OnViolation(Reaction.Error, "Maksymalna szerokość to 1044 mm.")))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(1100m))
            .Result;

        Assert.Equal(
            [new StepMessage(MessageKind.Error, "Maksymalna szerokość to 1044 mm.", "MaxSzerokosc", "SzerokoscMM")],
            result.Messages);
    }

    // A correction tells the user what changed; a warning reaction keeps the value and warns.
    [Theory]
    [InlineData(Reaction.Correct, MessageKind.Information)]
    [InlineData(Reaction.Warning, MessageKind.Warning)]
    [InlineData(Reaction.Error, MessageKind.Error)]
    public void Reaction_gives_the_kind_of_its_message(Reaction reaction, MessageKind kind)
    {
        var result = Start(catalog => catalog.Rule("MaxSzerokosc", rule => rule
                .Max("SzerokoscMM", Number(1044m))
                .OnViolation(reaction, "Maksymalna szerokość to 1044 mm.")))
            .Set("Model", Value.Option("PORTA"))
            .Set("SzerokoscMM", Value.Number(1100m))
            .Result;

        Assert.Equal([kind], result.Messages.Select(message => message.Kind));
    }

    [Fact]
    public void Result_has_the_rule_messages()
    {
        var result = Start(catalog => catalog.Rule("VerteInfo", rule => rule
                .When(ModelVerte)
                .Inform("Verte ma ościeżnicę regulowaną.")))
            .Set("Model", Value.Option("VERTE"))
            .Result;

        Assert.Equal(
            [new StepMessage(MessageKind.Information, "Verte ma ościeżnicę regulowaną.", "VerteInfo", null)],
            result.Messages);
    }

    [Theory]
    [InlineData(Severity.Information, MessageKind.Information)]
    [InlineData(Severity.Warning, MessageKind.Warning)]
    [InlineData(Severity.Error, MessageKind.Error)]
    public void Rule_message_keeps_its_severity(Severity severity, MessageKind kind)
    {
        var result = Start(catalog => catalog.Rule("VerteKomunikat", rule => severity switch
            {
                Severity.Information => rule.Inform("Verte."),
                Severity.Warning => rule.Warn("Verte."),
                _ => rule.Error("Verte."),
            }))
            .Set("Model", Value.Option("VERTE"))
            .Result;

        Assert.Equal([kind], result.Messages.Select(message => message.Kind));
    }

    [Fact]
    public void Result_has_the_changes_of_the_step()
    {
        var result = Start(catalog => catalog
                .Computed("SzerokoscOscieznicyMM", Add(Reference("SzerokoscMM"), Number(60m)))
                .Rule("VerteWaska", rule => rule.When(ModelVerte).Set("SzerokoscMM", Number(900m))))
            .Set("Model", Value.Option("VERTE"))
            .Result;

        Assert.Equal(
            [
                new ValueChange("SzerokoscMM", Value.Number(944m), Value.Number(900m), ChangeCause.Forced, "VerteWaska"),
                new ValueChange("SzerokoscOscieznicyMM", Value.Number(1004m), Value.Number(960m), ChangeCause.Computed, null),
            ],
            result.Changes);
    }

    // Complete means every visible feature has a value; errors are in the messages.
    [Fact]
    public void Result_is_complete_when_every_visible_feature_has_a_value()
    {
        var start = Start();

        Assert.False(start.Set("Model", Value.Option("PORTA")).Result.IsComplete);
        Assert.True(start.Set("Model", Value.Option("VERTE")).Result.IsComplete);
    }
}
