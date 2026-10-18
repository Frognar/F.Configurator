using F.Configurator.Dsl.Syntax;
using static F.Configurator.Dsl.Tests.Declarations;
using static F.Configurator.Dsl.Tests.Parsing;

namespace F.Configurator.Dsl.Tests;

// A declaration and each of its parts point at their name or first word, so later messages (repeated name,
// unknown property) can underline it.
public class DeclarationSpanTests
{
    private const string Source = """
        katalog "Drzwi"
        cecha Model : wybór
          prezentacja kafelki
          atrybut Seria : tekst
          opcja "13-VRATA D,P"
        """;

    private static IReadOnlyList<DeclarationSyntax> Read() => ParseFile(Source).Syntax!.Declarations;

    [Fact]
    public void Declaration_points_at_its_name()
    {
        Assert.Equal([At(1, 9, 7), At(2, 7, 5)], Read().Select(declaration => declaration.Span));
    }

    [Fact]
    public void Parts_of_a_feature_point_at_their_names()
    {
        var feature = (FeatureSyntax)Read()[1];

        Assert.Equal(At(3, 3, 11), Assert.Single(feature.Properties).Span);
        Assert.Equal(At(4, 11, 5), Assert.Single(feature.Attributes).Span);
        Assert.Equal(At(5, 9, 14), Assert.Single(feature.Options).Span);
    }

    [Fact]
    public void Parts_of_a_collection_rule_and_test_point_at_their_first_word()
    {
        const string Source = """
            kolekcja Demo
              etap "Model"
                cechy Model, Typ widoczna gdy wybrana(Model)
              reguła R
                wtedy ukryj A
            test "T"
              oczekuj komunikat R
            """;

        var declarations = ParseFile(Source).Syntax!.Declarations;
        var collection = (CollectionSyntax)declarations[0];
        var stage = Assert.Single(collection.Stages);
        var rule = (RuleSyntax)Assert.Single(collection.Declarations);
        var test = (TestSyntax)declarations[1];

        Assert.Equal(At(2, 8, 7), stage.Span);
        Assert.Equal([At(3, 11, 5), At(3, 18, 3)], stage.Features.Select(feature => feature.Span));
        Assert.Equal(At(4, 10, 1), rule.Span);
        Assert.Equal(At(5, 11, 5), Assert.Single(rule.Effects).Span);
        Assert.Equal(At(6, 6, 3), test.Span);
        Assert.Equal(At(7, 11, 9), Assert.Single(test.Expectations).Span);
    }

    [Fact]
    public void Parts_of_a_visualization_link_and_price_list_point_at_their_first_word()
    {
        const string Source = """
            kolekcja Demo
              wizualizacja
                warstwa "a.png"
            powiązanie P
              z A do B
              przejmij Kolor jako domyślne
            cennik C
              baza z "c.csv" klucz Model
              dopłata W5 "w"
                kwota 70
              nadpisz Ramiak kwota 30
              rabat 5%
            przypisz C do kontrahenta "X"
            """;

        var declarations = ParseFile(Source).Syntax!.Declarations;
        var collection = (CollectionSyntax)declarations[0];
        var link = (LinkSyntax)declarations[1];
        var priceList = (PriceListSyntax)declarations[2];

        Assert.Equal(At(3, 13, 7), Assert.Single(collection.Visualization!).Span);
        Assert.Equal(At(4, 12, 1), link.Span);
        Assert.Equal(At(6, 3, 8), Assert.Single(link.Takeovers).Span);
        Assert.Equal(At(7, 8, 1), priceList.Span);
        Assert.Equal(At(8, 3, 4), priceList.Base!.Span);
        Assert.Equal(At(9, 11, 2), Assert.Single(priceList.Surcharges).Span);
        Assert.Equal(At(11, 11, 6), Assert.Single(priceList.Overrides).Span);
        Assert.Equal(At(12, 3, 5), Assert.Single(priceList.Adjustments).Span);
        Assert.Equal(At(13, 10, 1), declarations[3].Span);
    }
}
