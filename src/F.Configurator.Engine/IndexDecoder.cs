using F.Configurator.Catalog;

namespace F.Configurator.Engine;

public static class IndexDecoder
{
    public static IReadOnlyList<Configuration> Decode(ProductCatalog catalog, string collectionName, string index) =>
        [];
}
