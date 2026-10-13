using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public static class IndexDecoder
{
    public static IReadOnlyList<Configuration> Decode(ProductCatalog catalog, string collectionName, string index)
    {
        var pattern = catalog.Collections.First(c => c.Name == collectionName).Index;
        if (pattern is null)
        {
            return [];
        }

        var configuration = Configuration.Start(catalog, collectionName);
        var rest = index;
        foreach (var symbol in Segments(pattern).Cast<OptionAttribute>())
        {
            var feature = ((Reference)symbol.Selected).Name;
            var option = catalog.Features.OfType<ChoiceFeature>().First(f => f.Name == feature).Options
                .FirstOrDefault(o => rest.StartsWith(SymbolOf(symbol, o.Id)));
            if (option is null)
            {
                return [];
            }

            configuration = configuration.Set(feature, Value.Option(option.Id));
            rest = rest[SymbolOf(symbol, option.Id).Length..];
        }

        return [configuration];
    }

    private static IEnumerable<Expression> Segments(Expression pattern) =>
        pattern is Add add ? Segments(add.Left).Concat(Segments(add.Right)) : [pattern];

    private static string SymbolOf(OptionAttribute symbol, string option) =>
        ((TextValue)symbol.ValuesByOption[option]).Content;
}
