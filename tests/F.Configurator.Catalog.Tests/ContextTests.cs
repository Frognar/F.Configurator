namespace F.Configurator.Catalog.Tests;

// Grammar 4.6 (D15): `kontekst Kontrahent` declares attributes of a value given by the platform,
// not by the user; rules read them as `Kontrahent.Asortyment`.
public class ContextTests
{
    [Fact]
    public void Builder_keeps_a_context_with_its_attributes()
    {
        var catalog = CatalogBuilder.Create("Drzwi")
            .Context("Kontrahent", context => context
                .Attribute("Asortyment", AttributeType.Text)
                .Attribute("Rynek", AttributeType.Text))
            .Build();

        var kontrahent = catalog.Contexts["Kontrahent"];
        Assert.Equal(
            [new AttributeDeclaration("Asortyment", AttributeType.Text, null), new AttributeDeclaration("Rynek", AttributeType.Text, null)],
            kontrahent.Attributes);
    }

    [Fact]
    public void Context_name_cannot_repeat()
    {
        var builder = CatalogBuilder.Create("Drzwi").Context("Kontrahent", context => context);

        Assert.Throws<ArgumentException>(() => builder.Context("Kontrahent", context => context));
    }
}
