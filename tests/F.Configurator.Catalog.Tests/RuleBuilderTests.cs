using F.Configurator.Expressions;

namespace F.Configurator.Catalog.Tests;

// Grammar 4.7: a rule is a name, an optional condition (`gdy`), one or more effects (`wtedy`),
// a priority and, optionally, the collections it applies to and a reaction to a violated value.
// The builder only records rules; combining their effects is the engine's job (analysis 2.3).
public class RuleBuilderTests
{
    [Fact]
    public void Builder_creates_a_rule_with_its_condition()
    {
        var condition = Expression.Equal(Expression.Reference("ZamekDolny"), Expression.Option("BRAK"));

        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("ZamekBrak", rule => rule.When(condition).Hide("ZmianaPolozeniaZamka"))
            .Build();

        var zamekBrak = Assert.Single(catalog.Rules);
        Assert.Equal("ZamekBrak", zamekBrak.Name);
        Assert.Same(condition, zamekBrak.Condition);
    }

    // Grammar 4.7: without `gdy` the rule always applies.
    [Fact]
    public void Rule_without_a_condition_has_none()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("ZawszeUkryj", rule => rule.Hide("Wentylacja1"))
            .Build();

        Assert.Null(Assert.Single(catalog.Rules).Condition);
    }

    // Grammar 4.7: `priorytet` is optional and defaults to 0.
    [Fact]
    public void Rule_priority_defaults_to_zero()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("ZawszeUkryj", rule => rule.Hide("Wentylacja1"))
            .Build();

        Assert.Equal(0, Assert.Single(catalog.Rules).Priority);
    }

    [Fact]
    public void Rule_keeps_its_priority()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("MaxSzerY", rule => rule.Priority(10).Max("SzerokoscMM", Expression.Number(1100m)))
            .Build();

        Assert.Equal(10, Assert.Single(catalog.Rules).Priority);
    }

    // Grammar 4.7: `dotyczy Brilliant, Basic`.
    [Fact]
    public void Rule_keeps_the_collections_it_applies_to()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("MaxSzerBasic", rule => rule
                .AppliesTo("Brilliant", "Basic")
                .Max("SzerokoscMM", Expression.Number(1044m)))
            .Build();

        Assert.Equal(["Brilliant", "Basic"], Assert.Single(catalog.Rules).Collections);
    }

    // Grammar 4.7: `ukryj A, B` and `zablokuj A, B`.
    [Fact]
    public void Rule_keeps_hide_and_lock_effects_in_order()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("FelcX", rule => rule
                .Hide("ZmianaPolozeniaZamka")
                .Lock("SzerokoscMM", "WysokoscMM"))
            .Build();

        var effects = Assert.Single(catalog.Rules).Effects;
        Assert.Equal(2, effects.Count);
        Assert.Equal(["ZmianaPolozeniaZamka"], Assert.IsType<HideEffect>(effects[0]).Features);
        Assert.Equal(["SzerokoscMM", "WysokoscMM"], Assert.IsType<LockEffect>(effects[1]).Features);
    }

    // Grammar 4.7: `tylko Norma [PL, CZ]` and `zabroń Kolor [X, Y]`.
    [Fact]
    public void Rule_keeps_allowed_and_forbidden_options()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("Rynek", rule => rule
                .Only("Norma", "PL", "CZ")
                .Forbid("Kolor", "BIALY"))
            .Build();

        var effects = Assert.Single(catalog.Rules).Effects;
        var only = Assert.IsType<OnlyEffect>(effects[0]);
        Assert.Equal("Norma", only.Feature);
        Assert.Equal([new OptionValue("PL"), new OptionValue("CZ")], only.Options);
        var forbid = Assert.IsType<ForbidEffect>(effects[1]);
        Assert.Equal("Kolor", forbid.Feature);
        Assert.Equal([new OptionValue("BIALY")], forbid.Options);
    }

    // Grammar 4.7: `min`, `maks`, `krok`; the right-hand side is an expression.
    [Fact]
    public void Rule_keeps_number_bounds_as_expressions()
    {
        var maks = Expression.Divide(Expression.Reference("WysokoscMM"), Expression.Number(10m));

        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("Zamek", rule => rule
                .Min("ZmianaPolozeniaZamka", Expression.Number(0m))
                .Max("ZmianaPolozeniaZamka", maks)
                .Step("ZmianaPolozeniaZamka", Expression.Number(5m)))
            .Build();

        var effects = Assert.Single(catalog.Rules).Effects;
        Assert.Equal(new MinEffect("ZmianaPolozeniaZamka", Expression.Number(0m)), effects[0]);
        Assert.Same(maks, Assert.IsType<MaxEffect>(effects[1]).Value);
        Assert.Equal(new StepEffect("ZmianaPolozeniaZamka", Expression.Number(5m)), effects[2]);
    }

    // Grammar 4.7: `domyślnie A = 2050` and `ustaw Podciecie = TAK`.
    [Fact]
    public void Rule_keeps_default_and_forced_values()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("WysDomyslnaY", rule => rule
                .Default("WysokoscMM", Expression.Number(2050m))
                .Set("Podciecie", Expression.Option("TAK")))
            .Build();

        var effects = Assert.Single(catalog.Rules).Effects;
        Assert.Equal(new DefaultEffect("WysokoscMM", Expression.Number(2050m)), effects[0]);
        Assert.Equal(new SetEffect("Podciecie", Expression.Option("TAK")), effects[1]);
    }

    // Grammar 4.7: `błąd "…"`, `ostrzeż "…"`, `informuj "…"`.
    [Fact]
    public void Rule_keeps_messages_with_their_severity()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("NiskaWysokosc", rule => rule
                .Warn("Wysokość poniżej 1970 mm wymaga uzgodnienia z działem technicznym."))
            .Build();

        var message = Assert.IsType<MessageEffect>(Assert.Single(Assert.Single(catalog.Rules).Effects));
        Assert.Equal(Severity.Warning, message.Severity);
        Assert.Equal("Wysokość poniżej 1970 mm wymaga uzgodnienia z działem technicznym.", message.Text);
    }

    // Grammar 4.7: `przy naruszeniu koryguj "…"`; without it the catalog's default reaction applies.
    [Fact]
    public void Rule_keeps_its_reaction_to_a_violation()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("MaxSzerBasic", rule => rule
                .Max("SzerokoscMM", Expression.Number(1044m))
                .OnViolation(Reaction.Correct, "Maksymalna szerokość to {maks} mm. Zmieniono wartość."))
            .Rule("BezReakcji", rule => rule.Max("WysokoscMM", Expression.Number(2100m)))
            .Build();

        var violation = catalog.Rules[0].Violation;
        Assert.NotNull(violation);
        Assert.Equal(Reaction.Correct, violation.Reaction);
        Assert.Equal("Maksymalna szerokość to {maks} mm. Zmieniono wartość.", violation.Message);
        Assert.Null(catalog.Rules[1].Violation);
    }

    // Grammar 4.7: `błąd "…"` and `informuj "…"` next to `ostrzeż "…"`.
    [Fact]
    public void Rule_keeps_error_and_information_messages()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("Komunikaty", rule => rule
                .Error("Ta kombinacja nie jest produkowana.")
                .Inform("Termin realizacji wydłuża się o tydzień."))
            .Build();

        var effects = Assert.Single(catalog.Rules).Effects;
        Assert.Equal(new MessageEffect(Severity.Error, "Ta kombinacja nie jest produkowana."), effects[0]);
        Assert.Equal(new MessageEffect(Severity.Information, "Termin realizacji wydłuża się o tydzień."), effects[1]);
    }

    // Analysis 2.5 (N5): `przy naruszeniu koryguj | błąd | ostrzeż`.
    [Theory]
    [InlineData(Reaction.Correct)]
    [InlineData(Reaction.Error)]
    [InlineData(Reaction.Warn)]
    public void Rule_keeps_every_kind_of_reaction(Reaction reaction)
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("MaxSzer", rule => rule
                .Max("SzerokoscMM", Expression.Number(1044m))
                .OnViolation(reaction, "Za szeroko."))
            .Build();

        Assert.Equal(reaction, Assert.Single(catalog.Rules).Violation?.Reaction);
    }

    // Grammar 4.4: `wtedy domyślnie SzerokoscMM, WysokoscMM z WymiaryDomyslne` (columns matched by name).
    [Fact]
    public void Rule_keeps_defaults_from_a_table()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("WymiaryZTabeli", rule => rule.DefaultsFrom("WymiaryDomyslne", "SzerokoscMM", "WysokoscMM"))
            .Build();

        var effect = Assert.IsType<DefaultsFromTableEffect>(Assert.Single(Assert.Single(catalog.Rules).Effects));
        Assert.Equal("WymiaryDomyslne", effect.Table);
        Assert.Equal(["SzerokoscMM", "WysokoscMM"], effect.Features);
    }

    // Grammar G18: `tylko Uchwyt z DozwoloneUchwyt`.
    [Fact]
    public void Rule_keeps_allowed_options_from_a_combinations_table()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Rule("Zaleznosci", rule => rule
                .OnlyFrom("Model", "DozwoloneModel")
                .OnlyFrom("Uchwyt", "DozwoloneUchwyt"))
            .Build();

        var effects = Assert.Single(catalog.Rules).Effects;
        Assert.Equal(new OnlyFromTableEffect("Model", "DozwoloneModel"), effects[0]);
        Assert.Equal(new OnlyFromTableEffect("Uchwyt", "DozwoloneUchwyt"), effects[1]);
    }
}
