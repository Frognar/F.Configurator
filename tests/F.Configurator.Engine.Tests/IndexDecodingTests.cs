using CsCheck;
using static F.Configurator.Engine.Tests.IndexPatterns;

namespace F.Configurator.Engine.Tests;

// Analysis 2.9 and decision 2026-10-13: the decoder tries every way the pattern can read the index (symbols, numbers,
// padded numbers, branches of conditions) and keeps the configurations that the engine encodes back to the same
// index. No configuration means an unknown index, more than one an ambiguous one.
public class IndexDecodingTests
{
    private static readonly Expression ModelSymbol =
        SymbolOf("Model", ("PORTA", "P"), ("PORTALUX", "PL"), ("VERTE", "V"));

    private static readonly Expression NormaSymbol = SymbolOf("Norma", ("PL", "PL"), ("CZ", "CZ"));
    private static readonly Expression ZamekSymbol = SymbolOf("ZamekDolny", ("KLUCZ", "K"), ("BRAK", "B"));

    // Basic writes the lock shift only for a lock: `jeśli ZamekDolny = BRAK to "" inaczej "Z" + przesunięcie`.
    private static readonly Expression Shift = If(
        Equal(Reference("ZamekDolny"), Option("BRAK")),
        Text(""),
        Add(Text("Z"), Reference("PrzesuniecieZamkaMM")));

    private static CatalogBuilder HeightUpTo2400(CatalogBuilder catalog) =>
        catalog.Rule("WysokoscDo2400", rule => rule.Max("WysokoscMM", Number(2400m)));

    private static ProductCatalog Catalog(Expression? pattern, Func<CatalogBuilder, CatalogBuilder>? addRules = null)
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature
                .Option("PORTA", "Porta", option => option.Symbol("P"))
                .Option("PORTALUX", "Porta Lux", option => option.Symbol("PL"))
                .Option("VERTE", "Verte", option => option.Symbol("V")))
            .Choice("Norma", feature => feature.Option("PL", "Polska").Option("CZ", "Czeska"))
            .Number("SzerokoscMM", feature => feature.Unit("mm"))
            .Number("WysokoscMM", feature => feature.Unit("mm"))
            .Choice("ZamekDolny", feature => feature
                .Option("KLUCZ", "Na klucz", option => option.Symbol("K"))
                .Option("BRAK", "Brak", option => option.Symbol("B")))
            .Number("PrzesuniecieZamkaMM", feature => feature.Unit("mm"))
            .Computed("SzerokoscOscieznicyMM", Add(Reference("SzerokoscMM"), Number(60m)))
            .Rule("SzerokoscDomyslna", rule => rule.Default("SzerokoscMM", Number(844m)))
            .Rule("WysokoscDomyslna", rule => rule.Default("WysokoscMM", Number(2030m)))
            .Rule("ZamekDomyslny", rule => rule.Default("ZamekDolny", Option("BRAK")))
            .Rule("BezZamkaBezPrzesuniecia", rule => rule
                .When(Equal(Reference("ZamekDolny"), Option("BRAK")))
                .Hide("PrzesuniecieZamkaMM"))
            .Collection("Basic", collection =>
            {
                var stages = collection
                    .Stage("Model", "Model", "Norma")
                    .Stage("Wymiary", "SzerokoscMM", "WysokoscMM")
                    .Stage("Okucia", "ZamekDolny", "PrzesuniecieZamkaMM");
                return pattern is null ? stages : stages.Index(pattern);
            });

        return (addRules?.Invoke(catalog) ?? catalog).Build();
    }

    private static IReadOnlyList<Configuration> Decode(
        Expression? pattern,
        string index,
        Func<CatalogBuilder, CatalogBuilder>? addRules = null) =>
        IndexDecoder.Decode(Catalog(pattern, addRules), "Basic", index);

    private static Expression Join(Expression first, params IEnumerable<Expression> rest) =>
        rest.Aggregate(first, Add);

    [Fact]
    public void Index_decodes_to_the_chosen_options()
    {
        var decoded = Decode(Join(ModelSymbol, NormaSymbol), "VCZ");

        Assert.Equal(
            [(Value.Option("VERTE"), Value.Option("CZ"))],
            decoded.Select(c => (c.ValueOf("Model"), c.ValueOf("Norma"))));
    }

    [Fact]
    public void Index_that_the_pattern_cannot_read_decodes_to_nothing()
    {
        Assert.Empty(Decode(Join(ModelSymbol, NormaSymbol), "XCZ"));
    }

    [Fact]
    public void Collection_without_a_pattern_decodes_nothing()
    {
        Assert.Empty(Decode(null, "VCZ"));
    }

    // "P" leaves "LCZ", which no symbol of Norma starts, so only "PL" + "CZ" reads the whole index.
    [Fact]
    public void Symbol_that_leads_nowhere_is_abandoned()
    {
        var decoded = Decode(Join(ModelSymbol, NormaSymbol), "PLCZ");

        Assert.Equal(
            [(Value.Option("PORTALUX"), Value.Option("CZ"))],
            decoded.Select(c => (c.ValueOf("Model"), c.ValueOf("Norma"))));
    }

    [Fact]
    public void Number_is_read_from_its_digits()
    {
        var decoded = Decode(Join(ModelSymbol, NormaSymbol, Reference("SzerokoscMM")), "VCZ900");

        Assert.Equal([Value.Number(900m)], decoded.Select(c => c.ValueOf("SzerokoscMM")));
    }

    [Fact]
    public void Padded_number_is_read_without_its_padding()
    {
        var decoded = Decode(Join(ModelSymbol, NormaSymbol, Pad(Reference("SzerokoscMM"), Number(4m))), "VCZ0900");

        Assert.Equal([Value.Number(900m)], decoded.Select(c => c.ValueOf("SzerokoscMM")));
    }

    // Two numbers without padding or a separator between them can be read in several ways; each one that encodes
    // back to the same index is a configuration of its own.
    [Fact]
    public void Index_read_in_several_ways_decodes_to_each_of_them()
    {
        var pattern = Join(ModelSymbol, NormaSymbol, Reference("SzerokoscMM"), Reference("WysokoscMM"));

        var decoded = Decode(pattern, "VCZ9002030");

        Assert.Equal(
            [
                (Value.Number(900m), Value.Number(2030m)),
                (Value.Number(90020m), Value.Number(30m)),
                (Value.Number(900203m), Value.Number(0m)),
            ],
            decoded
                .Select(c => (c.ValueOf("SzerokoscMM"), c.ValueOf("WysokoscMM")))
                .OrderBy(values => ((NumberValue)values.Item1).Amount));
    }

    [Fact]
    public void Condition_decodes_the_branch_that_reads_the_index()
    {
        var decoded = Decode(Join(ModelSymbol, NormaSymbol, ZamekSymbol, Shift), "VCZKZ-21");

        Assert.Equal(
            [(Value.Option("KLUCZ"), Value.Number(-21m))],
            decoded.Select(c => (c.ValueOf("ZamekDolny"), c.ValueOf("PrzesuniecieZamkaMM"))));
    }

    [Fact]
    public void Empty_branch_of_a_condition_decodes()
    {
        var decoded = Decode(Join(ModelSymbol, NormaSymbol, ZamekSymbol, Shift), "VCZB");

        Assert.Equal(
            [(Value.Option("BRAK"), Value.Missing)],
            decoded.Select(c => (c.ValueOf("ZamekDolny"), c.ValueOf("PrzesuniecieZamkaMM"))));
    }

    // "VCZK" reads with the empty branch, but a lock needs its shift: the configuration is not complete, so it has
    // no index and is not the one encoded as "VCZK".
    [Fact]
    public void Reading_the_engine_does_not_encode_back_is_rejected()
    {
        Assert.Empty(Decode(Join(ModelSymbol, NormaSymbol, ZamekSymbol, Shift), "VCZK"));
    }

    [Fact]
    public void Value_equal_to_its_default_stays_a_default()
    {
        var decoded = Decode(Join(ModelSymbol, NormaSymbol, Reference("SzerokoscMM")), "VCZ844");

        Assert.Equal([ValueSource.Default], decoded.Select(c => c.SourceOf("SzerokoscMM")));
    }

    // The engine computes the frame width, so the decoder does not set it; the frame width in the index only has to
    // match the computed one.
    [Fact]
    public void Computed_feature_in_the_index_is_checked_not_set()
    {
        var pattern = Join(
            ModelSymbol, NormaSymbol, Reference("SzerokoscMM"), Text("-"), Reference("SzerokoscOscieznicyMM"));

        Assert.Equal([Value.Number(900m)], Decode(pattern, "VCZ900-960").Select(c => c.ValueOf("SzerokoscMM")));
        Assert.Empty(Decode(pattern, "VCZ900-999"));
    }

    // The index may list features in another order than the stages; the decoder sets them stage by stage, as a user
    // would.
    [Fact]
    public void Values_are_set_in_the_order_of_the_stages()
    {
        var decoded = Decode(Join(Reference("SzerokoscMM"), ModelSymbol, NormaSymbol), "900VCZ");

        Assert.Equal(
            [(Value.Option("VERTE"), Value.Option("CZ"), Value.Number(900m))],
            decoded.Select(c => (c.ValueOf("Model"), c.ValueOf("Norma"), c.ValueOf("SzerokoscMM"))));
    }

    // The engine refuses an option a rule excludes, so no configuration has that index.
    [Fact]
    public void Reading_the_rules_do_not_allow_is_rejected()
    {
        var decoded = Decode(
            Join(ModelSymbol, NormaSymbol),
            "VCZ",
            catalog => catalog.Rule("VerteBezCZ", rule => rule
                .When(Equal(Reference("Model"), Option("VERTE")))
                .Forbid("Norma", "CZ")));

        Assert.Empty(decoded);
    }

    // "VCZBZ-21" reads with a shift, but without a lock the shift is hidden: the engine does not let it be set.
    [Fact]
    public void Reading_that_sets_a_hidden_feature_is_rejected()
    {
        Assert.Empty(Decode(Join(ModelSymbol, NormaSymbol, ZamekSymbol, Shift), "VCZBZ-21"));
    }

    // A padded height does not end the width: "6071969" also reads as 60 and 71969, which pads to itself. The height
    // limit rejects that reading: a configuration that breaks a rule cannot be finished, so it is not a product.
    [Fact]
    public void Reading_that_breaks_a_limit_is_rejected()
    {
        var pattern = Join(
            ModelSymbol, NormaSymbol, Reference("SzerokoscMM"), Pad(Reference("WysokoscMM"), Number(4m)));

        var decoded = Decode(pattern, "VCZ6071969", HeightUpTo2400);

        Assert.Equal(
            [(Value.Number(607m), Value.Number(1969m))],
            decoded.Select(c => (c.ValueOf("SzerokoscMM"), c.ValueOf("WysokoscMM"))));
    }

    private sealed record Door(string Model, string Norma, int Width, int Height, string Lock, int Shift);

    // Plan E4: decode(encode(x)) = x. The index identifies the product, so it decodes back to exactly the configuration
    // it came from, here with a Basic-like pattern: symbols of different length, a bare and a padded number, a shift
    // written only for a lock. As in Basic, the height limit keeps the two numbers apart.
    [Fact]
    public void Index_decodes_back_to_its_configuration()
    {
        var catalog = Catalog(Join(
            ModelSymbol,
            NormaSymbol,
            Reference("SzerokoscMM"),
            Pad(Reference("WysokoscMM"), Number(4m)),
            ZamekSymbol,
            Shift),
            HeightUpTo2400);
        var doors = Gen.Select(
            Gen.OneOfConst("PORTA", "PORTALUX", "VERTE"),
            Gen.OneOfConst("PL", "CZ"),
            Gen.Int[600, 1200],
            Gen.Int[1900, 2400],
            Gen.OneOfConst("KLUCZ", "BRAK"),
            Gen.Int[-50, 50],
            (model, norma, width, height, @lock, shift) => new Door(model, norma, width, height, @lock, shift));

        doors.Sample(door =>
        {
            var configuration = Configuration.Start(catalog, "Basic")
                .Set("Model", Value.Option(door.Model))
                .Set("Norma", Value.Option(door.Norma))
                .Set("SzerokoscMM", Value.Number(door.Width))
                .Set("WysokoscMM", Value.Number(door.Height))
                .Set("ZamekDolny", Value.Option(door.Lock));
            if (door.Lock == "KLUCZ")
            {
                configuration = configuration.Set("PrzesuniecieZamkaMM", Value.Number(door.Shift));
            }

            var decoded = IndexDecoder.Decode(catalog, "Basic", configuration.Index!);

            Assert.Equal([ValuesOf(configuration)], decoded.Select(ValuesOf));
        });
    }

    private static EquatableList<(string, Value)> ValuesOf(Configuration configuration) =>
        [.. configuration.Result.Features.Select(feature => (feature.Name, feature.Value))];
}
