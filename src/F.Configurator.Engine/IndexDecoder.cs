using System.Collections.Immutable;
using System.Globalization;
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

        var start = Configuration.Start(catalog, collectionName);
        return
        [
            .. Readings(catalog, pattern, index, [])
                .Where(reading => reading.Unread == "")
                .Select(reading => reading.Values.Aggregate(
                    start, (configuration, value) => configuration.Set(value.Feature, value.Value)))
        ];
    }

    // Each way the pattern can read the start of the text: the values it read and the text it left.
    private static IEnumerable<(ImmutableList<(string Feature, Value Value)> Values, string Unread)> Readings(
        ProductCatalog catalog,
        Expression pattern,
        string text,
        ImmutableList<(string Feature, Value Value)> values) =>
        pattern switch
        {
            Add add => Readings(catalog, add.Left, text, values)
                .SelectMany(reading => Readings(catalog, add.Right, reading.Unread, reading.Values)),
            OptionAttribute { Selected: Reference feature } symbol => catalog.Features.OfType<ChoiceFeature>()
                .First(f => f.Name == feature.Name).Options
                .Select(option => (Option: option.Id, Symbol: SymbolOf(symbol, option.Id)))
                .Where(x => text.StartsWith(x.Symbol))
                .Select(x => (values.Add((feature.Name, Value.Option(x.Option))), text[x.Symbol.Length..])),
            Reference feature => Enumerable.Range(1, text.TakeWhile(char.IsDigit).Count())
                .Select(length => (
                    values.Add(
                        (feature.Name, Value.Number(decimal.Parse(text[..length], CultureInfo.InvariantCulture)))),
                    text[length..])),
            _ => [],
        };

    private static string SymbolOf(OptionAttribute symbol, string option) =>
        ((TextValue)symbol.ValuesByOption[option]).Content;
}
