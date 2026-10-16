using F.Configurator.Dsl.Syntax;
using static F.Configurator.Dsl.Tests.Declarations;
using static F.Configurator.Dsl.Tests.Parsing;

namespace F.Configurator.Dsl.Tests;

// A declaration, an option, an attribute and a property point at their name, so later messages (repeated name,
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
}
