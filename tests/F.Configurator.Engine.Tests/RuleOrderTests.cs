using CsCheck;

namespace F.Configurator.Engine.Tests;

// Plan E4: the engine is deterministic with respect to the order of the rules. A catalog is written in several files
// and the rules of one file may be moved around, so the order in which rules are declared must not change anything
// the user sees: values, sources, available options and the rules named for them, limits, messages and changes.
public class RuleOrderTests
{
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));
    private static readonly Expression ModelLux = Equal(Reference("Model"), Option("LUX"));

    // Rules that agree in what they do but differ in their names, reactions and messages, so their order could leak
    // into the result.
    private static readonly Func<CatalogBuilder, CatalogBuilder>[] Rules =
    [
        catalog => catalog.Rule("VerteBezDebu", rule => rule
            .When(ModelVerte)
            .Forbid("Kolor", "DAB")
            .OnViolation(Reaction.Error, "Verte nie ma koloru dąb.")),
        catalog => catalog.Rule("DabTylkoDoPorty", rule => rule
            .When(ModelVerte)
            .Forbid("Kolor", "DAB")
            .OnViolation(Reaction.Correct, "Dąb jest tylko do Porty.")),
        catalog => catalog.Rule("KolorBialy", rule => rule.Default("Kolor", Option("BIALY"))),
        catalog => catalog.Rule("KolorStandardowy", rule => rule.Default("Kolor", Option("BIALY"))),
        catalog => catalog.Rule("SzerokoscDomyslna", rule => rule.Default("SzerokoscMM", Number(900m))),
        catalog => catalog.Rule("MaksymalnaSzerokosc", rule => rule
            .Max("SzerokoscMM", Number(1000m))
            .OnViolation(Reaction.Correct, "Maksymalna szerokość to 1000 mm.")),
        catalog => catalog.Rule("SzerokoscProdukcyjna", rule => rule
            .Max("SzerokoscMM", Number(1000m))
            .OnViolation(Reaction.Warning, "Szerokość ponad 1000 mm wymaga zgody produkcji.")),
        catalog => catalog.Rule("LuxBezKoloru", rule => rule.When(ModelLux).Hide("Kolor")),
        catalog => catalog.Rule("LuxTylkoNaturalny", rule => rule.When(ModelLux).Hide("Kolor")),
        catalog => catalog.Rule("VerteInformacja", rule => rule.When(ModelVerte).Inform("Verte ma ukryte zawiasy.")),
        catalog => catalog.Rule("VerteOstrzezenie", rule => rule.When(ModelVerte).Warn("Verte ma dłuższy termin.")),
    ];

    private static ProductCatalog Catalog(IEnumerable<Func<CatalogBuilder, CatalogBuilder>> rules) =>
        rules.Aggregate(
                CatalogBuilder.Create("Drzwi")
                    .Choice("Model", feature => feature
                        .Option("PORTA", "Porta")
                        .Option("VERTE", "Verte")
                        .Option("LUX", "Lux"))
                    .Choice("Kolor", feature => feature
                        .Option("BIALY", "Biały")
                        .Option("DAB", "Dąb")
                        .Option("ORZECH", "Orzech"))
                    .Number("SzerokoscMM", feature => feature.Unit("mm"))
                    .Collection("Basic", collection => collection
                        .Stage("Model", "Model")
                        .Stage("Wykonczenie", "Kolor", "SzerokoscMM")),
                (catalog, addRule) => addRule(catalog))
            .Build();

    private sealed record Step(string Feature, Value Value);

    // What the user sees after a step: the step result, what the engine allows next and why, or the refusal.
    private sealed record Seen(
        StepResult? Result,
        EquatableList<UnavailableOption>? UnavailableColours,
        NumberLimits? WidthLimits,
        bool? CanFinish,
        string? Refusal);

    private static EquatableList<Seen> Walk(ProductCatalog catalog, IEnumerable<Step> steps)
    {
        var configuration = Configuration.Start(catalog, "Basic");
        var seen = new List<Seen> { SeenIn(configuration) };
        foreach (var step in steps)
        {
            try
            {
                configuration = configuration.Set(step.Feature, step.Value);
                seen.Add(SeenIn(configuration));
            }
            catch (InvalidOperationException refusal)
            {
                seen.Add(new Seen(null, null, null, null, refusal.GetType().Name));
            }
        }

        return [.. seen];
    }

    private static Seen SeenIn(Configuration configuration) =>
        new(configuration.Result,
            configuration.UnavailableOptions("Kolor"),
            configuration.LimitsOf("SzerokoscMM"),
            configuration.CanFinish,
            null);

    private static readonly Gen<Step> Model = Gen.OneOfConst("PORTA", "VERTE", "LUX")
        .Select(model => new Step("Model", Value.Option(model)));

    private static readonly Gen<Step> Colour = Gen.OneOfConst("BIALY", "DAB", "ORZECH")
        .Select(colour => new Step("Kolor", Value.Option(colour)));

    private static readonly Gen<Step> Width = Gen.Int[700, 1200]
        .Select(width => new Step("SzerokoscMM", Value.Number(width)));

    [Fact]
    public void Order_of_the_rules_does_not_change_what_the_user_sees()
    {
        var steps = Gen.Select(Model, Gen.OneOf(Colour, Width).Array[0, 4], Model,
            (first, middle, last) => (Step[])[first, .. middle, last]);

        Gen.Select(Gen.Shuffle(Rules), steps).Sample((rules, walk) =>
            Assert.Equal(Walk(Catalog(Rules), walk), Walk(Catalog(rules), walk)));
    }
}
