using System.Collections.Immutable;
using System.Globalization;
using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public static class IndexDecoder
{
    public static IReadOnlyList<Configuration> Decode(ProductCatalog catalog, string collectionName, string index)
    {
        var collection = catalog.Collections.First(c => c.Name == collectionName);
        if (collection.Index is null)
        {
            return [];
        }

        var start = Configuration.Start(catalog, collectionName);
        return
        [
            .. Readings(catalog, collection.Index, index, [])
                .Where(reading => reading.Unread == "")
                .Select(reading => SetAll(
                    start,
                    collection.Features.SelectMany(feature => reading.Values.Where(value => value.Feature == feature))))
                .OfType<Configuration>()
                .Where(configuration => configuration.Index == index && configuration.CanFinish)
        ];
    }

    // The engine refuses a value the rules do not allow, so such a reading has no configuration.
    private static Configuration? SetAll(Configuration start, IEnumerable<(string Feature, Value Value)> values)
    {
        try
        {
            return values.Aggregate(
                start,
                (configuration, value) => configuration.ValueOf(value.Feature) == value.Value
                    ? configuration
                    : configuration.Set(value.Feature, value.Value));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
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
            Reference feature => NumberLengths(text)
                .Select(length => (
                    values.Add(
                        (feature.Name, Value.Number(decimal.Parse(text[..length], CultureInfo.InvariantCulture)))),
                    text[length..])),
            Pad pad => Readings(catalog, pad.Operand, text, values),
            If conditional => Readings(catalog, conditional.Then, text, values)
                .Concat(Readings(catalog, conditional.Otherwise, text, values)),
            Text literal => text.StartsWith(literal.Content) ? [(values, text[literal.Content.Length..])] : [],
            _ => [],
        };

    // Lengths of the numbers the text starts with: digits after an optional minus.
    private static IEnumerable<int> NumberLengths(string text)
    {
        var sign = text.StartsWith('-') ? 1 : 0;
        return Enumerable.Range(sign + 1, text[sign..].TakeWhile(char.IsDigit).Count());
    }

    private static string SymbolOf(OptionAttribute symbol, string option) =>
        ((TextValue)symbol.ValuesByOption[option]).Content;
}
