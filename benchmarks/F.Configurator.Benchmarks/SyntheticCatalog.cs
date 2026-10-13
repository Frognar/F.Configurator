namespace F.Configurator.Benchmarks;

// Analysis 3.4: a catalog of the size of a real one, made up in code: 40 features in 8 stages, about 400 rules and an
// allowed combinations table of 2048 rows. The rules chain the choices one after another, so a change at the start
// reaches the whole configuration.
public static class SyntheticCatalog
{
    public const string Collection = "Pelna";

    private const int Choices = 30;
    private const int OptionsPerChoice = 8;
    private const int Numbers = 6;
    private const int Texts = 4;

    public static string ChoiceName(int i) => $"C{i:00}";

    public static string NumberName(int i) => $"N{i}";

    public static string TextName(int i) => $"T{i}";

    public static string OptionId(int i) => $"O{i}";

    public static ProductCatalog Build()
    {
        var catalog = CatalogBuilder.Create("Syntetyczny");
        catalog = Enumerable.Range(0, Choices).Aggregate(catalog, AddChoice);
        catalog = Enumerable.Range(0, Numbers).Aggregate(catalog, AddNumber);
        catalog = Enumerable.Range(0, Texts).Aggregate(catalog, AddText);
        catalog = AddCombinations(catalog);

        return catalog
            .Collection(Collection, collection => Stages().Aggregate(
                collection,
                (c, stage) => c.Stage(stage.Name, stage.Features[0], stage.Features[1..])))
            .Build();
    }

    private static IEnumerable<(string Name, string[] Features)> Stages() =>
    [
        .. Enumerable.Range(0, Choices).Chunk(5)
            .Select((chunk, i) => ($"Wybory{i}", chunk.Select(ChoiceName).ToArray())),
        ("Wymiary", [.. Enumerable.Range(0, Numbers).Select(NumberName)]),
        ("Napisy", [.. Enumerable.Range(0, Texts).Select(TextName)]),
    ];

    private static Expression Chosen(string feature, int option) => Equal(Reference(feature), Option(OptionId(option)));

    // Every option of the previous choice forbids one option of this one, and one of them changes its default.
    private static CatalogBuilder AddChoice(CatalogBuilder catalog, int i)
    {
        var name = ChoiceName(i);
        catalog = catalog
            .Choice(name, feature => Enumerable.Range(0, OptionsPerChoice)
                .Aggregate(feature, (f, o) => f.Option(OptionId(o), $"Opcja {o}")))
            .Rule($"{name}Domyslna", rule => rule.Default(name, Option(OptionId(0))))
            .Rule($"{name}Informacja", rule => rule.When(Chosen(name, OptionsPerChoice - 1)).Inform($"Wybrano {name}."));

        if (i == 0)
        {
            return catalog;
        }

        var previous = ChoiceName(i - 1);
        catalog = catalog.Rule($"{name}DomyslnaPo{previous}", rule => rule
            .When(Chosen(previous, 3))
            .Priority(1)
            .Default(name, Option(OptionId(2))));

        return Enumerable.Range(0, OptionsPerChoice).Aggregate(catalog, (c, o) => c
            .Rule($"{name}Bez{(o + 1) % OptionsPerChoice}Po{previous}{o}", rule => rule
                .When(Chosen(previous, o))
                .Forbid(name, OptionId((o + 1) % OptionsPerChoice))
                .OnViolation(Reaction.Correct, $"{name} nie ma tej opcji.")));
    }

    private static CatalogBuilder AddNumber(CatalogBuilder catalog, int i)
    {
        var name = NumberName(i);
        var choice = ChoiceName(i);
        return catalog
            .Number(name, feature => feature.Unit("mm").Min(0m).Max(5000m))
            .Rule($"{name}Domyslna", rule => rule.Default(name, Number(1000m)))
            .Rule($"{name}Co5", rule => rule.Step(name, Number(5m)).OnViolation(Reaction.Correct, "Co 5 mm."))
            .Rule($"{name}Waska", rule => rule
                .When(Chosen(choice, 1))
                .Max(name, Number(800m))
                .OnViolation(Reaction.Correct, "Do 800 mm."))
            .Rule($"{name}Szeroka", rule => rule
                .When(Chosen(choice, 2))
                .Min(name, Number(500m))
                .OnViolation(Reaction.Warning, "Od 500 mm wymaga zgody."));
    }

    private static CatalogBuilder AddText(CatalogBuilder catalog, int i)
    {
        var name = TextName(i);
        return catalog
            .Text(name)
            .Rule($"{name}Domyslny", rule => rule.Default(name, Text("ABC")))
            .Rule($"{name}Krotki", rule => rule
                .When(Chosen(ChoiceName(i), 3))
                .Length(name, Range(1m, 5m))
                .OnViolation(Reaction.Correct, "Do 5 znaków."));
    }

    // The first three choices allow half of the options of the fourth one: 512 keys with 4 options each.
    private static CatalogBuilder AddCombinations(CatalogBuilder catalog)
    {
        var keys =
            from a in Enumerable.Range(0, OptionsPerChoice)
            from b in Enumerable.Range(0, OptionsPerChoice)
            from c in Enumerable.Range(0, OptionsPerChoice)
            from k in Enumerable.Range(0, OptionsPerChoice / 2)
            select (a, b, c, Allowed: (a + b + c + (2 * k)) % OptionsPerChoice);

        return catalog
            .AllowedCombinations(
                "Kombinacje",
                [ChoiceName(0), ChoiceName(1), ChoiceName(2)],
                ChoiceName(3),
                table => keys.Aggregate(table, (t, x) =>
                    t.Row(OptionId(x.a), OptionId(x.b), OptionId(x.c), OptionId(x.Allowed))))
            .Rule("Kombinacje", rule => rule.OnlyFrom(ChoiceName(3), "Kombinacje"));
    }
}
