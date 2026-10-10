namespace F.Configurator.Catalog.Tests;

// E3 completion criterion (plan, E3): the Basic fragment from analysis 2.4 written with the builder.
// Left out on purpose: `Nietyp` and `OdchylkaSzerokosc` need `domyślna(…)`, which is engine work (E4.4).
// The builder does not check references (e.g. `Typ` is not declared here); that is the validator (E5).
public class BasicFragmentTests
{
    private static readonly Table WymiaryDomyslne = new(
        ["Norma", "Szerokosc"],
        [
            new TableRow(
                [[Value.Option("PL")], [Value.Option("80")]],
                new Dictionary<string, Value> { ["SzerokoscMM"] = Value.Number(844m), ["WysokoscMM"] = Value.Number(2030m) }.ToEquatableDictionary()),
        ]);

    private static readonly EquatableDictionary<string, Value> Seria = new Dictionary<string, Value>
    {
        ["STANDARD_01"] = Value.Text("STANDARD"),
        ["ASTORIA_01"] = Value.Text("ASTORIA"),
    }.ToEquatableDictionary();

    private static Catalog Basic() =>
        CatalogBuilder.Create("Drzwi wewnętrzne")
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Number("WysokoscMM", feature => feature.Unit("mm"))
            .Table("WymiaryDomyslne", WymiaryDomyslne)
            .Rule("WymiaryZTabeli", rule => rule.AppliesTo("Basic").DefaultsFrom(["SzerokoscMM", "WysokoscMM"], "WymiaryDomyslne"))
            .Rule("ZamekBrak", rule => rule.AppliesTo("Basic")
                .When(Equal(Reference("ZamekDolny"), Option("BRAK")))
                .Hide("ZmianaPolozeniaZamka"))
            .Rule("FelcXWymiary", rule => rule.AppliesTo("Basic")
                .When(Equal(Reference("FelcSkrzydla"), Option("X")))
                .Lock("SzerokoscMM", "WysokoscMM"))
            .Rule("MaxSzerBasic", rule => rule.AppliesTo("Brilliant", "Basic")
                .When(In(Reference("Kolekcja"), Expression.List(Option("Brilliant"), Option("Basic"))))
                .Max("SzerokoscMM", Number(1044m))
                .OnViolation(Reaction.Correct, "Maksymalna dopuszczalna szerokość dla tego modelu to {maks} mm. Zmieniono wartość."))
            .Rule("NiskaWysokoscBasic", rule => rule.AppliesTo("Basic")
                .When(And(
                    NotEqual(OptionAttribute(Reference("Model"), Seria), Text("ASTORIA")),
                    LessThan(Reference("WysokoscMM"), Number(1970m))))
                .Warn("Wysokość poniżej 1970 mm wymaga uzgodnienia z działem technicznym."))
            .Collection("Basic", collection => collection
                .Stage("Model", "Model", "Typ", "FelcSkrzydla")
                .Stage("Wymiary", "Norma", "Szerokosc", "SzerokoscMM", "WysokoscMM")
                .Stage("Okucia", "ZamekDolny", "ZmianaPolozeniaZamka")
                .Index(Add(Add(Add(Reference("Model"), Reference("Typ")), Reference("SzerokoscMM")), Pad(Reference("WysokoscMM"), Number(4m)))))
            .Build();

    [Fact]
    public void Basic_fragment_has_its_features_rules_and_collection()
    {
        var catalog = Basic();

        Assert.Equal(["SzerokoscMM", "WysokoscMM"], catalog.Features.Select(feature => feature.Name));
        Assert.Equal(
            ["WymiaryZTabeli", "ZamekBrak", "FelcXWymiary", "MaxSzerBasic", "NiskaWysokoscBasic"],
            catalog.Rules.Select(rule => rule.Name));
        var basic = Assert.Single(catalog.Collections);
        Assert.Equal(9, basic.Features.Count);
        Assert.NotNull(basic.Index);
    }

    [Fact]
    public void Basic_width_limit_applies_to_basic_and_corrects_the_value()
    {
        var maxSzer = Basic().Rules.Single(rule => rule.Name == "MaxSzerBasic");
        var values = new Dictionary<string, Value> { ["Kolekcja"] = Value.Option("Basic") };

        Assert.Equal(Value.Boolean(true), maxSzer.Condition!.Evaluate(values));
        Assert.Equal(new MaxEffect("SzerokoscMM", Number(1044m)), Assert.Single(maxSzer.Effects));
        Assert.Equal(Reaction.Correct, maxSzer.Violation?.Reaction);
    }

    // Grammar 2.4: the low-height warning skips the ASTORIA series.
    [Theory]
    [InlineData("STANDARD_01", 1950, true)]
    [InlineData("ASTORIA_01", 1950, false)]
    [InlineData("STANDARD_01", 2030, false)]
    public void Low_height_warning_depends_on_series_and_height(string model, int wysokoscMM, bool expected)
    {
        var niskaWysokosc = Basic().Rules.Single(rule => rule.Name == "NiskaWysokoscBasic");
        var values = new Dictionary<string, Value>
        {
            ["Model"] = Value.Option(model),
            ["WysokoscMM"] = Value.Number(wysokoscMM),
        };

        Assert.Equal(Value.Boolean(expected), niskaWysokosc.Condition!.Evaluate(values));
    }

    // The DSL compiler (E5) will be tested by comparing whole catalogs, so every record in the catalog
    // compares by content: features, options, rules, effects, collections, stages and tables.
    [Fact]
    public void Catalogs_built_the_same_way_are_equal()
    {
        StructuralAssert.Equal(Basic(), Basic());
    }
}
