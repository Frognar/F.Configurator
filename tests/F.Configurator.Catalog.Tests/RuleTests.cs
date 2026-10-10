namespace F.Configurator.Catalog.Tests;

// Grammar 4.7: a rule is a name, an optional condition (`gdy`), one or more effects (`wtedy`),
// a priority and, optionally, the collections it applies to and a reaction to a violated value.
// The builder only records rules; combining their effects is the engine's job (analysis 2.3).
public class RuleTests
{
    [Fact]
    public void Builder_creates_a_rule_with_its_condition()
    {
        var condition = Equal(Reference("ZamekDolny"), Option("BRAK"));

        var rule = RuleBuilder.Create("ZamekBrak").When(condition).Hide("ZmianaPolozeniaZamka").Build();

        Assert.Equal("ZamekBrak", rule.Name);
        Assert.Same(condition, rule.Condition);
    }

    // Grammar 4.7: without `gdy` the rule always applies.
    [Fact]
    public void Rule_without_a_condition_has_none()
    {
        var rule = RuleBuilder.Create("ZawszeUkryj").Hide("Wentylacja1").Build();

        Assert.Null(rule.Condition);
    }

    // Grammar 4.7: `priorytet` is optional and defaults to 0.
    [Fact]
    public void Rule_priority_defaults_to_zero()
    {
        var rule = RuleBuilder.Create("ZawszeUkryj").Hide("Wentylacja1").Build();

        Assert.Equal(0, rule.Priority);
    }

    [Fact]
    public void Rule_keeps_its_priority()
    {
        var rule = RuleBuilder.Create("MaxSzerY").Priority(10).Max("SzerokoscMM", Number(1100m)).Build();

        Assert.Equal(10, rule.Priority);
    }

    // Grammar 4.7: `dotyczy Brilliant, Basic`.
    [Fact]
    public void Rule_keeps_the_collections_it_applies_to()
    {
        var rule = RuleBuilder.Create("MaxSzerBasic")
            .AppliesTo("Brilliant", "Basic")
            .Max("SzerokoscMM", Number(1044m))
            .Build();

        Assert.Equal(["Brilliant", "Basic"], rule.Collections);
    }

    // Grammar 4.7: `ukryj A, B` and `zablokuj A, B`.
    [Fact]
    public void Rule_keeps_hide_and_lock_effects_in_order()
    {
        var rule = RuleBuilder.Create("FelcX")
            .Hide("ZmianaPolozeniaZamka")
            .Lock("SzerokoscMM", "WysokoscMM")
            .Build();

        Assert.Equal(
            [new HideEffect(["ZmianaPolozeniaZamka"]), new LockEffect(["SzerokoscMM", "WysokoscMM"])],
            rule.Effects);
    }

    // Grammar 4.7: `tylko Norma [PL, CZ]` and `zabroń Kolor [X, Y]`.
    [Fact]
    public void Rule_keeps_allowed_and_forbidden_options()
    {
        var rule = RuleBuilder.Create("Rynek")
            .Only("Norma", "PL", "CZ")
            .Forbid("Kolor", "BIALY")
            .Build();

        Assert.Equal(
            [
                new OnlyEffect("Norma", [Value.Option("PL"), Value.Option("CZ")]),
                new ForbidEffect("Kolor", [Value.Option("BIALY")]),
            ],
            rule.Effects);
    }

    // Grammar 4.7: `min`, `maks`, `krok`; the right-hand side is an expression.
    [Fact]
    public void Rule_keeps_number_bounds_as_expressions()
    {
        var maks = Divide(Reference("WysokoscMM"), Number(10m));

        var rule = RuleBuilder.Create("Zamek")
            .Min("ZmianaPolozeniaZamka", Number(0m))
            .Max("ZmianaPolozeniaZamka", maks)
            .Step("ZmianaPolozeniaZamka", Number(5m))
            .Build();

        Assert.Equal(
            [
                new MinEffect("ZmianaPolozeniaZamka", Number(0m)),
                new MaxEffect("ZmianaPolozeniaZamka", maks),
                new StepEffect("ZmianaPolozeniaZamka", Number(5m)),
            ],
            rule.Effects);
    }

    // Grammar 4.7: `domyślnie A = 2050` and `ustaw Podciecie = TAK`.
    [Fact]
    public void Rule_keeps_default_and_forced_values()
    {
        var rule = RuleBuilder.Create("WysDomyslnaY")
            .Default("WysokoscMM", Number(2050m))
            .Set("Podciecie", Option("TAK"))
            .Build();

        Assert.Equal(
            [
                new DefaultEffect("WysokoscMM", Number(2050m)),
                new SetEffect("Podciecie", Option("TAK")),
            ],
            rule.Effects);
    }

    // Grammar 4.7: `błąd "…"`, `ostrzeż "…"`, `informuj "…"`.
    [Fact]
    public void Rule_keeps_messages_with_their_severity()
    {
        var rule = RuleBuilder.Create("Komunikaty")
            .Error("Ta kombinacja nie jest produkowana.")
            .Warn("Wysokość poniżej 1970 mm wymaga uzgodnienia z działem technicznym.")
            .Inform("Termin realizacji wydłuża się o tydzień.")
            .Build();

        Assert.Equal(
            [
                new MessageEffect(Severity.Error, "Ta kombinacja nie jest produkowana."),
                new MessageEffect(Severity.Warning, "Wysokość poniżej 1970 mm wymaga uzgodnienia z działem technicznym."),
                new MessageEffect(Severity.Information, "Termin realizacji wydłuża się o tydzień."),
            ],
            rule.Effects);
    }

    // Grammar 4.7: `przy naruszeniu koryguj "…"`.
    [Fact]
    public void Rule_keeps_its_reaction_to_a_violation()
    {
        var rule = RuleBuilder.Create("MaxSzerBasic")
            .Max("SzerokoscMM", Number(1044m))
            .OnViolation(Reaction.Correct, "Maksymalna szerokość to {maks} mm. Zmieniono wartość.")
            .Build();

        Assert.Equal(new Violation(Reaction.Correct, "Maksymalna szerokość to {maks} mm. Zmieniono wartość."), rule.Violation);
    }

    // Without `przy naruszeniu` the catalog's default reaction applies.
    [Fact]
    public void Rule_without_a_reaction_has_none()
    {
        var rule = RuleBuilder.Create("BezReakcji").Max("WysokoscMM", Number(2100m)).Build();

        Assert.Null(rule.Violation);
    }

    // Analysis 2.5 (N5): `przy naruszeniu koryguj | błąd | ostrzeż`.
    [Theory]
    [InlineData(Reaction.Correct)]
    [InlineData(Reaction.Error)]
    [InlineData(Reaction.Warning)]
    public void Rule_keeps_every_kind_of_reaction(Reaction reaction)
    {
        var rule = RuleBuilder.Create("MaxSzer")
            .Max("SzerokoscMM", Number(1044m))
            .OnViolation(reaction, "Za szeroko.")
            .Build();

        Assert.Equal(reaction, rule.Violation?.Reaction);
    }

    // Grammar 4.4: `wtedy domyślnie SzerokoscMM, WysokoscMM z WymiaryDomyslne` (columns matched by name).
    // Features come first and the table last, as in the grammar and in `OnlyFrom` (`tylko X z T`).
    [Fact]
    public void Rule_keeps_defaults_from_a_table()
    {
        var rule = RuleBuilder.Create("WymiaryZTabeli")
            .DefaultsFrom(["SzerokoscMM", "WysokoscMM"], "WymiaryDomyslne")
            .Build();

        Assert.Equal([new DefaultsFromTableEffect(["SzerokoscMM", "WysokoscMM"], "WymiaryDomyslne")], rule.Effects);
    }

    // Grammar G18: `tylko Uchwyt z DozwoloneUchwyt`.
    [Fact]
    public void Rule_keeps_allowed_options_from_a_combinations_table()
    {
        var rule = RuleBuilder.Create("Zaleznosci")
            .OnlyFrom("Model", "DozwoloneModel")
            .OnlyFrom("Uchwyt", "DozwoloneUchwyt")
            .Build();

        Assert.Equal(
            [new OnlyFromTableEffect("Model", "DozwoloneModel"), new OnlyFromTableEffect("Uchwyt", "DozwoloneUchwyt")],
            rule.Effects);
    }

    // The catalog keeps its rules in declaration order; the engine sorts them by priority.
    [Fact]
    public void Catalog_keeps_rules_in_declaration_order()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("ZamekBrak", rule => rule.Hide("ZmianaPolozeniaZamka"))
            .Rule("MaxSzer", rule => rule.Max("SzerokoscMM", Number(1044m)))
            .Build();

        Assert.Equal(["ZamekBrak", "MaxSzer"], catalog.Rules.Select(rule => rule.Name));
    }
}
