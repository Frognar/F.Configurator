using static F.Configurator.Dsl.Tests.Declarations;

namespace F.Configurator.Dsl.Tests;

// Grammar 4.8: a link proposes a second product (a frame for a door leaf) and takes values over from the first.
public class LinkParserTests
{
    private static string Takeover(string takeover) => Tree($"powiązanie P\n  z A do B\n  przejmij {takeover}");

    [Fact]
    public void Link_joins_two_collections()
    {
        const string Source = """
            powiązanie OscieznicaDoSkrzydla
              z Basic do OscieznicaRegulowana
            """;

        Assert.Equal("(powiązanie OscieznicaDoSkrzydla (z Basic do OscieznicaRegulowana))", Tree(Source));
    }

    [Fact]
    public void Link_may_be_proposed_only_when_a_condition_holds()
    {
        const string Source = """
            powiązanie P
              z Basic do OscieznicaRegulowana
              proponuj gdy Kontrahent.Asortyment w [STANDARD, PREMIUM]
            """;

        Assert.Equal(
            "(powiązanie P (z Basic do OscieznicaRegulowana) (proponuj (w (. Kontrahent Asortyment) [STANDARD PREMIUM])))",
            Tree(Source));
    }

    [Theory]
    [InlineData("Kolor jako domyślne", "(przejmij Kolor jako domyślne)")]
    [InlineData("Kierunek, SzerokoscMM jako zablokowane", "(przejmij Kierunek SzerokoscMM jako zablokowane)")]
    [InlineData("SzerokoscMM jako SzerokoscSkrzydlaMM", "(przejmij SzerokoscMM jako SzerokoscSkrzydlaMM)")]
    [InlineData("Czynne.SzerokoscMM jako zablokowane", "(przejmij (. Czynne SzerokoscMM) jako zablokowane)")]
    public void Link_takes_values_over(string takeover, string tree)
    {
        Assert.Equal($"(powiązanie P (z A do B) {tree})", Takeover(takeover));
    }
}
