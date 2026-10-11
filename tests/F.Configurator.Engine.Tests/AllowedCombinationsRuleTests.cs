namespace F.Configurator.Engine.Tests;

// Grammar G18 (analysis/basic/NOTATKA.md, 5): `tylko C z T` takes the allowed options of C from the
// rows of table T that match the current values of its key columns.
public class AllowedCombinationsRuleTests
{
    private static Configuration Start(Func<CatalogBuilder, CatalogBuilder>? addRules = null)
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
            .Choice("ZamekDolny", feature => feature.Option("KLUCZ", "Na klucz").Option("BRAK", "Brak"))
            .Choice("Uchwyt", feature => feature.Option("KLAMKA", "Klamka").Option("GALKA", "Gałka").Option("POCHWYT", "Pochwyt"))
            .AllowedCombinations("DozwoloneUchwyt", ["Model", "ZamekDolny"], "Uchwyt", table => table
                .Row("PORTA", "KLUCZ", "KLAMKA")
                .Row("PORTA", "KLUCZ", "GALKA")
                .Row("PORTA", "BRAK", "GALKA")
                .Row("VERTE", "KLUCZ", "KLAMKA")
                .Row("VERTE", "KLUCZ", "POCHWYT"))
            .Rule("UchwytZTabeli", rule => rule.OnlyFrom("Uchwyt", "DozwoloneUchwyt"))
            .Collection("Basic", collection => collection
                .Stage("Skrzydlo", "Model", "ZamekDolny")
                .Stage("Okucia", "Uchwyt"));

        return Configuration.Start((addRules?.Invoke(catalog) ?? catalog).Build(), "Basic");
    }

    // G18.1: allowed options are the values of C in all rows matching the key.
    [Fact]
    public void Options_come_from_the_rows_matching_the_key()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("ZamekDolny", Value.Option("KLUCZ"));

        Assert.Equal(["KLAMKA", "GALKA"], configuration.AvailableOptions("Uchwyt"));
    }

    [Fact]
    public void Option_missing_from_the_rows_names_the_rule()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("ZamekDolny", Value.Option("KLUCZ"));

        Assert.Equal([new UnavailableOption("POCHWYT", "UchwytZTabeli")], configuration.UnavailableOptions("Uchwyt"));
    }

    // G18.2: while a key feature has no value the constraint waits.
    [Fact]
    public void Table_narrows_nothing_until_every_key_feature_has_a_value()
    {
        var configuration = Start().Set("Model", Value.Option("PORTA"));

        Assert.Equal(["KLAMKA", "GALKA", "POCHWYT"], configuration.AvailableOptions("Uchwyt"));
    }

    // G18.2: waiting is not "no row", so the feature is not hidden while its key is incomplete.
    [Fact]
    public void Feature_stays_visible_until_every_key_feature_has_a_value()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
            .Choice("Uchwyt", feature => feature.Option("KLAMKA", "Klamka").Option("GALKA", "Gałka"))
            .AllowedCombinations("DozwoloneUchwyt", ["Model"], "Uchwyt", table => table.Row("PORTA", "GALKA"))
            .Rule("UchwytZTabeli", rule => rule.OnlyFrom("Uchwyt", "DozwoloneUchwyt"))
            .Collection("Basic", collection => collection.Stage("Drzwi", "Model", "Uchwyt"))
            .Build();

        var configuration = Configuration.Start(catalog, "Basic");

        Assert.Equal(["Model", "Uchwyt"], configuration.VisibleFeatures);
    }

    // G18.1: a table combines with other `tylko` rules by intersection.
    [Fact]
    public void Table_and_only_rule_leave_the_options_they_share()
    {
        var configuration = Start(catalog => catalog.Rule("BezKlamki", rule => rule.Only("Uchwyt", "GALKA", "POCHWYT")))
            .Set("Model", Value.Option("PORTA"))
            .Set("ZamekDolny", Value.Option("KLUCZ"));

        Assert.Equal(["GALKA"], configuration.AvailableOptions("Uchwyt"));
    }

    [Fact]
    public void Setting_an_option_the_table_does_not_allow_is_rejected()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("ZamekDolny", Value.Option("KLUCZ"));

        Assert.Throws<InvalidOperationException>(() => configuration.Set("Uchwyt", Value.Option("POCHWYT")));
    }

    // G18.3: no row for the key means the feature does not apply: it is hidden and empty.
    [Fact]
    public void Feature_without_a_row_for_the_key_is_hidden()
    {
        var configuration = Start()
            .Set("Model", Value.Option("VERTE"))
            .Set("ZamekDolny", Value.Option("BRAK"));

        Assert.DoesNotContain("Uchwyt", configuration.VisibleFeatures);
    }

    [Fact]
    public void Feature_that_loses_its_rows_loses_its_value()
    {
        var configuration = Start()
            .Set("Model", Value.Option("VERTE"))
            .Set("ZamekDolny", Value.Option("KLUCZ"))
            .Set("Uchwyt", Value.Option("KLAMKA"))
            .Set("ZamekDolny", Value.Option("BRAK"));

        Assert.Equal(Value.Missing, configuration.ValueOf("Uchwyt"));
    }

    // G18.4: when the table allows exactly one option, that option becomes the value.
    [Fact]
    public void Single_allowed_option_becomes_the_value()
    {
        var configuration = Start()
            .Set("Model", Value.Option("PORTA"))
            .Set("ZamekDolny", Value.Option("BRAK"));

        Assert.Equal(Value.Option("GALKA"), configuration.ValueOf("Uchwyt"));
    }

    // A rule that does not apply leaves the table out, like any other effect.
    [Fact]
    public void Table_of_a_rule_that_does_not_match_narrows_nothing()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Choice("Model", feature => feature.Option("PORTA", "Porta").Option("VERTE", "Verte"))
            .Choice("Uchwyt", feature => feature.Option("KLAMKA", "Klamka").Option("GALKA", "Gałka"))
            .AllowedCombinations("DozwoloneUchwyt", ["Model"], "Uchwyt", table => table.Row("PORTA", "GALKA"))
            .Rule("UchwytZTabeli", rule => rule
                .When(Equal(Reference("Model"), Option("VERTE")))
                .OnlyFrom("Uchwyt", "DozwoloneUchwyt"))
            .Collection("Basic", collection => collection.Stage("Drzwi", "Model", "Uchwyt"))
            .Build();

        var configuration = Configuration.Start(catalog, "Basic").Set("Model", Value.Option("PORTA"));

        Assert.Equal(["KLAMKA", "GALKA"], configuration.AvailableOptions("Uchwyt"));
    }
}
