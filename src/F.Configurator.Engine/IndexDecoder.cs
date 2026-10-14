using System.Globalization;
using F.Configurator.Catalog;
using F.Configurator.Expressions;

namespace F.Configurator.Engine;

public static class IndexDecoder
{
    public static IReadOnlyList<Configuration> Decode(ProductCatalog catalog, string collectionName, string index) =>
        Decode(catalog, collectionName, index, EquatableDictionary<string, Value>.Empty);

    public static IReadOnlyList<Configuration> Decode(
        ProductCatalog catalog,
        string collectionName,
        string index,
        IReadOnlyDictionary<string, Value> context)
    {
        var collection = catalog.Collections.First(c => c.Name == collectionName);
        if (collection.Index is null)
        {
            return [];
        }

        var start = Configuration.Start(catalog, collectionName, context);
        return
        [
            .. Readings(catalog, collection.Index, new IndexReading([], index))
                .Where(reading => reading.Unread == "")
                .Select(reading => ConfigurationOf(start, collection, reading))
                .OfType<Configuration>()
                .Where(configuration => configuration.Index == index && configuration.CanFinish)
        ];
    }

    // Each way the pattern can read the start of the unread text.
    private static IEnumerable<IndexReading> Readings(
        ProductCatalog catalog,
        Expression pattern,
        IndexReading reading) =>
        pattern switch
        {
            Add add => Readings(catalog, add.Left, reading).SelectMany(left => Readings(catalog, add.Right, left)),
            OptionAttribute { Selected: Reference feature } symbols =>
                SymbolReadings(catalog, feature.Name, symbols, reading),
            Reference feature when TextsOf(catalog, feature.Name) is { } texts => texts
                .Where(text => reading.Unread.StartsWith(text, StringComparison.Ordinal))
                .Select(text => reading.Skip(text.Length)),
            Reference feature => NumberReadings(feature.Name, reading),
            Pad pad => Readings(catalog, pad.Operand, reading),
            If conditional => Readings(catalog, conditional.Then, reading)
                .Concat(Readings(catalog, conditional.Otherwise, reading)),
            Text literal => reading.Unread.StartsWith(literal.Content) ? [reading.Skip(literal.Content.Length)] : [],
            _ => [],
        };

    // Texts a computed feature can have, like "T" and "N" of `Nietyp`; null when its expression is not made of texts.
    private static IEnumerable<string>? TextsOf(ProductCatalog catalog, string feature) =>
        catalog.Features.OfType<ComputedFeature>().FirstOrDefault(f => f.Name == feature) is { } computed
            ? TextsOf(computed.Expression)
            : null;

    private static IEnumerable<string>? TextsOf(Expression expression) =>
        expression switch
        {
            Text text => [text.Content],
            If conditional when (TextsOf(conditional.Then), TextsOf(conditional.Otherwise)) is ({ } then, { } otherwise) =>
                then.Concat(otherwise),
            _ => null,
        };

    private static IEnumerable<IndexReading> SymbolReadings(
        ProductCatalog catalog,
        string feature,
        OptionAttribute symbols,
        IndexReading reading) =>
        catalog.Features.OfType<ChoiceFeature>().First(f => f.Name == feature).Options
            .Select(option => (option.Id, Symbol: ((TextValue)symbols.ValuesByOption[option.Id]).Content))
            .Where(x => reading.Unread.StartsWith(x.Symbol))
            .Select(x => reading.Read(feature, Value.Option(x.Id), x.Symbol.Length));

    // Numbers the unread text starts with: digits after an optional minus.
    private static IEnumerable<IndexReading> NumberReadings(string feature, IndexReading reading)
    {
        var sign = reading.Unread.StartsWith('-') ? 1 : 0;
        return Enumerable.Range(sign + 1, reading.Unread[sign..].TakeWhile(char.IsDigit).Count())
            .Select(length => reading.Read(
                feature, Value.Number(decimal.Parse(reading.Unread[..length], CultureInfo.InvariantCulture)), length));
    }

    // Sets the values stage by stage, as a user would. Features outside the collection, such as computed ones, are only
    // checked by encoding back. The engine refuses a value the rules do not allow, so such a reading has no
    // configuration.
    private static Configuration? ConfigurationOf(Configuration start, Collection collection, IndexReading reading)
    {
        try
        {
            return collection.Features
                .SelectMany(feature => reading.Values.Where(value => value.Feature == feature))
                .Aggregate(
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
}
