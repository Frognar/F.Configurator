using static F.Configurator.Engine.Tests.IndexPatterns;

namespace F.Configurator.Engine.Tests;

// Plan M2: a whole collection built with the builder goes through the engine from the first choice to the index.
// The catalog is made up but shaped like Basic: stages, allowed combinations, default dimensions from a table, limits
// with a priority exception, a computed `Nietyp`, optional features and an index with conditional segments. Real
// catalog data never goes into the repository.
public class DoorLeafScenarioTests
{
    private static readonly ProductCatalog Catalog = BuildCatalog();

    private static ProductCatalog BuildCatalog()
    {
        var dimensions = TableBuilder.Create(["Norma", "Felc", "Szerokosc"], ["SzerokoscMM", "WysokoscMM"]);
        foreach (var felc in new[] { "F40", "F30" })
        {
            dimensions = dimensions
                .Row([Value.Option("PL"), Value.Option(felc), Value.Option("70")], [Value.Number(744m), Value.Number(2030m)])
                .Row([Value.Option("PL"), Value.Option(felc), Value.Option("80")], [Value.Number(844m), Value.Number(2030m)])
                .Row([Value.Option("PL"), Value.Option(felc), Value.Option("90")], [Value.Number(944m), Value.Number(2030m)]);
        }

        dimensions = dimensions
            .Row([Value.Option("PL"), Value.Option("BP"), Value.Option("80")], [Value.Number(824m), Value.Number(2040m)])
            .Row([Value.Option("PL"), Value.Option("BP"), Value.Option("90")], [Value.Number(924m), Value.Number(2040m)]);
        foreach (var felc in new[] { "F40", "F30", "BP" })
        {
            dimensions = dimensions
                .Row([Value.Option("CZ"), Value.Option(felc), Value.Option("80")], [Value.Number(834m), Value.Number(1985m)])
                .Row([Value.Option("CZ"), Value.Option(felc), Value.Option("90")], [Value.Number(934m), Value.Number(1985m)]);
        }

        var index = new[]
        {
            Text("SKR"),
            SymbolOf("Model", ("LUNA", "LU"), ("SOLA", "SO"), ("NOVA", "NV")),
            SymbolOf("Kolor", ("BIEL", "BI"), ("ORZECH", "OR"), ("GRAFIT", "GR")),
            SymbolOf("Felc", ("F40", "40"), ("F30", "30"), ("BP", "BP")),
            If(HasValue(Reference("KrawedzBP")), SymbolOf("KrawedzBP", ("TAK", "T"), ("NIE", "N")), Text("")),
            SymbolOf("Norma", ("PL", "PL"), ("CZ", "CZ")),
            SymbolOf("Szerokosc", ("70", "70"), ("80", "80"), ("90", "90")),
            Reference("Nietyp"),
            Reference("SzerokoscMM"),
            Pad(Reference("WysokoscMM"), Number(4m)),
            SymbolOf("Zamek", ("BRAK", "0"), ("KLUCZ", "K"), ("WKLADKA", "W")),
            If(
                HasValue(Reference("PrzesuniecieZamka")),
                Add(Text("ZM"), Reference("PrzesuniecieZamka")),
                Text("")),
            SymbolOf("Zawiasy", ("2", "2"), ("3", "3")),
        }.Aggregate(Add);

        return CatalogBuilder.Create("Skrzydła demo")
            .DefaultReaction(Reaction.Error)
            .Choice("Model", feature => feature
                .Option("LUNA", "Luna", option => option.Symbol("LU"))
                .Option("SOLA", "Sola", option => option.Symbol("SO"))
                .Option("NOVA", "Nova", option => option.Symbol("NV")))
            .Choice("Kolor", feature => feature
                .Option("BIEL", "Biel", option => option.Symbol("BI"))
                .Option("ORZECH", "Orzech", option => option.Symbol("OR"))
                .Option("GRAFIT", "Grafit", option => option.Symbol("GR")))
            .Choice("Felc", feature => feature
                .Option("F40", "Felc 40 mm", option => option.Symbol("40"))
                .Option("F30", "Felc 30 mm", option => option.Symbol("30"))
                .Option("BP", "Bezprzylgowe"))
            .Choice("KrawedzBP", feature => feature
                .Option("TAK", "Tak", option => option.Symbol("T"))
                .Option("NIE", "Nie", option => option.Symbol("N"))
                .Optional())
            .Choice("Norma", feature => feature.Option("PL", "Polska").Option("CZ", "Czeska"))
            .Choice("Szerokosc", feature => feature.Option("70", "70 cm").Option("80", "80 cm").Option("90", "90 cm"))
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Number("WysokoscMM", feature => feature.Unit("mm"))
            .Computed("Nietyp", If(
                Or(
                    NotEqual(Reference("SzerokoscMM"), DefaultOf("SzerokoscMM")),
                    NotEqual(Reference("WysokoscMM"), DefaultOf("WysokoscMM"))),
                Text("T"),
                Text("N")))
            .Choice("Zamek", feature => feature
                .Option("BRAK", "Brak", option => option.Symbol("0"))
                .Option("KLUCZ", "Na klucz", option => option.Symbol("K"))
                .Option("WKLADKA", "Na wkładkę", option => option.Symbol("W")))
            .Number("PrzesuniecieZamka", feature => feature.Unit("mm").Optional())
            .Choice("Zawiasy", feature => feature.Option("2", "2 zawiasy").Option("3", "3 zawiasy"))
            .AllowedCombinations("DozwoloneKolor", ["Model"], "Kolor", table => table
                .Row("LUNA", "BIEL").Row("LUNA", "ORZECH")
                .Row("SOLA", "BIEL").Row("SOLA", "GRAFIT")
                .Row("NOVA", "BIEL").Row("NOVA", "ORZECH").Row("NOVA", "GRAFIT"))
            .AllowedCombinations("DozwoloneKrawedzBP", ["Felc"], "KrawedzBP", table => table.Row("BP", "TAK"))
            .AllowedCombinations("DozwoloneSzerokosc", ["Felc", "Norma"], "Szerokosc", table => table
                .Row("F40", "PL", "70").Row("F40", "PL", "80").Row("F40", "PL", "90")
                .Row("F40", "CZ", "80").Row("F40", "CZ", "90")
                .Row("F30", "PL", "80").Row("F30", "PL", "90")
                .Row("F30", "CZ", "80")
                .Row("BP", "PL", "80").Row("BP", "PL", "90")
                .Row("BP", "CZ", "80").Row("BP", "CZ", "90"))
            .Table("WymiaryDomyslne", dimensions.Build())
            .Rule("Zaleznosci", rule => rule
                .OnlyFrom("Kolor", "DozwoloneKolor")
                .OnlyFrom("KrawedzBP", "DozwoloneKrawedzBP")
                .OnlyFrom("Szerokosc", "DozwoloneSzerokosc")
                .OnViolation(Reaction.Correct, "Poprzedni wybór nie pasuje do zmienionych cech."))
            .Rule("Wymiary", rule => rule.DefaultsFrom(["SzerokoscMM", "WysokoscMM"], "WymiaryDomyslne"))
            .Rule("Domyslne", rule => rule
                .Default("Norma", Option("PL"))
                .Default("Zamek", Option("KLUCZ"))
                .Default("Zawiasy", Option("3")))
            .Rule("SzerokoscZakres", rule => rule
                .Min("SzerokoscMM", Subtract(DefaultOf("SzerokoscMM"), Number(50m)))
                .Max("SzerokoscMM", Add(DefaultOf("SzerokoscMM"), Number(50m)))
                .OnViolation(Reaction.Correct, "Szerokość może różnić się od domyślnej o najwyżej 50 mm."))
            .Rule("WysokoscZakres", rule => rule
                .Min("WysokoscMM", Subtract(DefaultOf("WysokoscMM"), Number(200m)))
                .Max("WysokoscMM", Add(DefaultOf("WysokoscMM"), Number(300m)))
                .OnViolation(Reaction.Error, "Wysokość poza dozwolonym zakresem."))
            .Rule("WysokoscZakresCZ", rule => rule
                .Priority(10)
                .When(Equal(Reference("Norma"), Option("CZ")))
                .Max("WysokoscMM", Add(DefaultOf("WysokoscMM"), Number(400m)))
                .OnViolation(Reaction.Error, "Wysokość poza dozwolonym zakresem."))
            .Rule("ZamekBrak", rule => rule
                .When(Equal(Reference("Zamek"), Option("BRAK")))
                .Hide("PrzesuniecieZamka"))
            .Collection("Skrzydla", collection => collection
                .Stage("Model i kolor", "Model", "Kolor")
                .Stage("Wykonanie", "Felc", "KrawedzBP")
                .Stage("Wymiary", "Norma", "Szerokosc", "SzerokoscMM", "WysokoscMM", "Nietyp")
                .Stage("Okucia", "Zamek", "PrzesuniecieZamka", "Zawiasy")
                .Index(index))
            .Build();
    }

    private static Configuration Configure(params (string Feature, Value Value)[] values) =>
        values.Aggregate(
            Configuration.Start(Catalog, "Skrzydla"),
            (configuration, value) => configuration.Set(value.Feature, value.Value));

    private static Configuration Standard() => Configure(
        ("Model", Value.Option("LUNA")),
        ("Kolor", Value.Option("ORZECH")),
        ("Felc", Value.Option("F40")),
        ("Szerokosc", Value.Option("80")));

    private static Configuration Bezprzylgowe() => Configure(
        ("Model", Value.Option("SOLA")),
        ("Kolor", Value.Option("GRAFIT")),
        ("Felc", Value.Option("BP")),
        ("Szerokosc", Value.Option("90")),
        ("Zamek", Value.Option("KLUCZ")),
        ("PrzesuniecieZamka", Value.Number(-15m)));

    // Dimensions from the table, defaults for the rest, the edge hidden and the optional lock shift left empty: a
    // finished leaf with an index.
    [Fact]
    public void Standard_leaf_gets_default_dimensions_and_an_index()
    {
        var configuration = Standard();

        Assert.Equal(
            (Value.Number(844m), Value.Number(2030m), Value.Text("N")),
            (configuration.ValueOf("SzerokoscMM"), configuration.ValueOf("WysokoscMM"), configuration.ValueOf("Nietyp")));
        Assert.True(configuration.CanFinish);
        Assert.Equal("SKRLUOR40PL80N8442030K3", configuration.Index);
    }

    // The only edge the table allows is chosen, and both optional segments are written.
    [Fact]
    public void Flush_leaf_gets_its_edge_and_writes_the_lock_shift()
    {
        var configuration = Bezprzylgowe();

        Assert.Equal(Value.Option("TAK"), configuration.ValueOf("KrawedzBP"));
        Assert.Equal("SKRSOGRBPTPL90N9242040KZM-153", configuration.Index);
    }

    // Too wide for the limit: the width is corrected with a message and the leaf becomes non-standard.
    [Fact]
    public void Too_wide_leaf_is_corrected_and_becomes_non_standard()
    {
        var configuration = Configure(
            ("Model", Value.Option("LUNA")),
            ("Kolor", Value.Option("BIEL")),
            ("Felc", Value.Option("F30")),
            ("Norma", Value.Option("CZ")),
            ("Szerokosc", Value.Option("80")),
            ("SzerokoscMM", Value.Number(900m)));

        Assert.Equal(
            (Value.Number(884m), Value.Text("T")),
            (configuration.ValueOf("SzerokoscMM"), configuration.ValueOf("Nietyp")));
        Assert.Contains(configuration.Result.Messages, message => message.RuleName == "SzerokoscZakres");
        Assert.Equal("SKRLUBI30CZ80T8841985K3", configuration.Index);
    }

    // The CZ exception has the higher priority, so it allows a height the general limit does not, but not more.
    [Theory]
    [InlineData(2300, true)]
    [InlineData(2400, false)]
    public void Height_limit_for_CZ_wins_over_the_general_one(int height, bool canFinish)
    {
        var configuration = Configure(
            ("Model", Value.Option("NOVA")),
            ("Kolor", Value.Option("GRAFIT")),
            ("Felc", Value.Option("F40")),
            ("Norma", Value.Option("CZ")),
            ("Szerokosc", Value.Option("90")),
            ("WysokoscMM", Value.Number(height)));

        Assert.Equal(canFinish, configuration.CanFinish);
    }

    // Luna has no graphite colour: after the model changes, the colour is cleared and the wizard goes back to it.
    [Fact]
    public void Colour_the_new_model_does_not_allow_is_cleared()
    {
        var configuration = Configure(
                ("Model", Value.Option("NOVA")),
                ("Kolor", Value.Option("GRAFIT")),
                ("Felc", Value.Option("F40")))
            .Set("Model", Value.Option("LUNA"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Kolor"));
        Assert.Equal("Model i kolor", configuration.CurrentStage);
    }

    [Fact]
    public void Index_of_a_standard_leaf_decodes_back_to_it()
    {
        AssertDecodesBackTo(Standard());
    }

    [Fact]
    public void Index_of_a_flush_leaf_decodes_back_to_it()
    {
        AssertDecodesBackTo(Bezprzylgowe());
    }

    private static void AssertDecodesBackTo(Configuration configuration)
    {
        var decoded = Assert.Single(IndexDecoder.Decode(Catalog, "Skrzydla", configuration.Index!));

        Assert.Equal(
            configuration.Result.Features.Select(f => (f.Name, f.Value)),
            decoded.Result.Features.Select(f => (f.Name, f.Value)));
    }
}
