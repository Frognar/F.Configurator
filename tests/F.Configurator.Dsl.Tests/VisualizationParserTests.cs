using static F.Configurator.Dsl.Tests.Declarations;

namespace F.Configurator.Dsl.Tests;

// Grammar 4.10: the visualization of a collection is a list of image layers; the engine returns the layers that apply
// and the interface draws them.
public class VisualizationParserTests
{
    [Fact]
    public void Visualization_lists_its_layers_in_order()
    {
        const string Source = """
            kolekcja Demo
              wizualizacja
                warstwa "basic/{Model}.png"
                warstwa "klamki/{Klamka}.png"
            """;

        Assert.Equal(
            "(kolekcja Demo (wizualizacja (warstwa \"basic/{Model}.png\") (warstwa \"klamki/{Klamka}.png\")))",
            Tree(Source));
    }

    [Fact]
    public void Layer_may_be_shown_only_when_a_condition_holds()
    {
        const string Source = """
            kolekcja Demo
              wizualizacja
                warstwa "zamki/{ZamekDolny}.png" gdy ZamekDolny ≠ BRAK
            """;

        Assert.Equal(
            "(kolekcja Demo (wizualizacja (warstwa \"zamki/{ZamekDolny}.png\" (gdy (≠ ZamekDolny BRAK)))))",
            Tree(Source));
    }

    [Fact]
    public void Layer_may_have_a_texture_and_be_mirrored()
    {
        const string Source = """
            kolekcja Demo
              wizualizacja
                warstwa "basic/{Model}.png"
                  tekstura Kolor.Tekstura
                  odbij gdy Kierunek = L
            """;

        Assert.Equal(
            "(kolekcja Demo (wizualizacja (warstwa \"basic/{Model}.png\" (tekstura (. Kolor Tekstura)) (odbij gdy (= Kierunek L)))))",
            Tree(Source));
    }
}
