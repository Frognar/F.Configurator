using CsCheck;

namespace F.Configurator.Engine.Tests;

// Analysis 3.3: no visible value lies outside its domain without a message, whatever the user does. The catalog mixes
// defaults outside the domain, values forced against other rules and every reaction to a value out of its limits.
public class DomainPropertyTests
{
    private static readonly Expression ModelPorta = Equal(Reference("Model"), Option("PORTA"));
    private static readonly Expression ModelVerte = Equal(Reference("Model"), Option("VERTE"));
    private static readonly Expression ModelLux = Equal(Reference("Model"), Option("LUX"));

    private static readonly ProductCatalog Catalog = CatalogBuilder.Create("Drzwi")
        .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte").Option("LUX", "Lux"))
        .Choice("Kolor", feature => feature.Option("BIALY", "Biały").Option("DAB", "Dąb").Option("ORZECH", "Orzech"))
        .Number("SzerokoscMM", feature => feature.Unit("mm").Min(600m))
        .Text("Napis")
        .Rule("KolorDab", rule => rule.Priority(1).Default("Kolor", Option("DAB")))
        .Rule("KolorBialy", rule => rule.Default("Kolor", Option("BIALY")))
        .Rule("VerteBezDebu", rule => rule
            .When(ModelVerte)
            .Forbid("Kolor", "DAB")
            .OnViolation(Reaction.Correct, "Verte nie ma koloru dąb."))
        .Rule("LuxTylkoOrzech", rule => rule
            .When(ModelLux)
            .Only("Kolor", "ORZECH")
            .OnViolation(Reaction.Error, "Lux jest tylko w orzechu."))
        .Rule("LuxBialy", rule => rule.When(ModelLux).Set("Kolor", Option("BIALY")))
        .Rule("SzerokoscDomyslna", rule => rule.Default("SzerokoscMM", Number(905m)))
        .Rule("MaksymalnaSzerokosc", rule => rule
            .Max("SzerokoscMM", Number(1000m))
            .OnViolation(Reaction.Correct, "Maksymalna szerokość to 1000 mm."))
        .Rule("VerteWaska", rule => rule
            .When(ModelVerte)
            .Priority(1)
            .Max("SzerokoscMM", Number(800m))
            .OnViolation(Reaction.Warning, "Verte ponad 800 mm wymaga zgody."))
        .Rule("LuxCoDziesiec", rule => rule
            .When(ModelLux)
            .Step("SzerokoscMM", Number(10m))
            .OnViolation(Reaction.Correct, "Lux ma szerokość co 10 mm."))
        .Rule("PortaSzeroka", rule => rule.When(ModelPorta).Set("SzerokoscMM", Number(1100m)))
        .Rule("NapisDomyslny", rule => rule.Default("Napis", Text("WEJSCIE")))
        .Rule("VerteKrotkiNapis", rule => rule
            .When(ModelVerte)
            .Length("Napis", Range(1m, 5m))
            .OnViolation(Reaction.Correct, "Napis na Verte ma do 5 znaków."))
        .Collection("Basic", collection => collection
            .Stage("Model", "Model")
            .Stage("Wykonczenie", "Kolor", "SzerokoscMM", "Napis"))
        .Build();

    private sealed record Step(string Feature, Value Value);

    private static readonly Gen<Step> Model = Gen.OneOfConst("PORTA", "VERTE", "LUX")
        .Select(model => new Step("Model", Value.Option(model)));

    private static readonly Gen<Step> Colour = Gen.OneOfConst("BIALY", "DAB", "ORZECH")
        .Select(colour => new Step("Kolor", Value.Option(colour)));

    private static readonly Gen<Step> Width = Gen.Int[500, 1300]
        .Select(width => new Step("SzerokoscMM", Value.Number(width)));

    private static readonly Gen<Step> Inscription = Gen.Char["ABCDEFGH"].Array[1, 8]
        .Select(letters => new Step("Napis", Value.Text(new string(letters))));

    [Fact]
    public void No_visible_value_lies_outside_its_domain_without_a_message()
    {
        var steps = Gen.OneOf(Model, Colour, Width, Inscription).Array[1, 8];

        steps.Sample(walk =>
        {
            var configuration = Configuration.Start(Catalog, "Basic");
            AssertWithinDomainOrExplained(configuration);
            foreach (var step in walk)
            {
                try
                {
                    configuration = configuration.Set(step.Feature, step.Value);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                AssertWithinDomainOrExplained(configuration);
            }
        });
    }

    private static void AssertWithinDomainOrExplained(Configuration configuration)
    {
        var outside = configuration.VisibleFeatures
            .Where(feature => !WithinDomain(configuration, feature, configuration.ValueOf(feature)))
            .Where(feature => configuration.Violations.All(violation => violation.Feature != feature));

        Assert.Empty(outside);
    }

    private static bool WithinDomain(Configuration configuration, string feature, Value value) => value switch
    {
        MissingValue => true,
        OptionValue option => configuration.AvailableOptions(feature).Contains(option.Id),
        NumberValue number => configuration.LimitsOf(feature) is var limits &&
                              !(number.Amount < limits.Min) &&
                              !(number.Amount > limits.Max) &&
                              (limits.Step is not { } step || number.Amount % step == 0),
        TextValue text => configuration.LengthOf(feature) is var length &&
                          !(text.Content.Length < length.Min) &&
                          !(text.Content.Length > length.Max),
        _ => true,
    };
}
