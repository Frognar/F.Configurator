namespace F.Configurator.Catalog.Tests;

// Grammar 4.6 (D15): `kontekst Kontrahent` declares attributes of a value given by the platform,
// not by the user; rules read them as `Kontrahent.Asortyment`.
public class ContextTests
{
    [Fact]
    public void Context_keeps_its_attributes()
    {
        var kontrahent = ContextBuilder.Create("Kontrahent")
            .Attribute("Asortyment", AttributeType.Text)
            .Attribute("Rynek", AttributeType.Text)
            .Build();

        Assert.Equal(
            [new AttributeDeclaration("Asortyment", AttributeType.Text, null), new AttributeDeclaration("Rynek", AttributeType.Text, null)],
            kontrahent.Attributes);
    }

    [Fact]
    public void Catalog_keeps_its_contexts_by_name()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Context("Kontrahent", context => context.Attribute("Rynek", AttributeType.Text))
            .Build();

        Assert.Equal("Kontrahent", catalog.Contexts["Kontrahent"].Name);
    }
}
