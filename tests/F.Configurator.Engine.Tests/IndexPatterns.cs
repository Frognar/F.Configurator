namespace F.Configurator.Engine.Tests;

// Decided 2026-10-13: in an index pattern an option gives its symbol through an explicit node, the existing
// OptionAttribute with the symbol of each option. The DSL compiler will fill the map; tests write it by hand.
internal static class IndexPatterns
{
    public static Expression SymbolOf(string feature, params IEnumerable<(string Option, string Symbol)> symbols) =>
        OptionAttribute(
            Reference(feature),
            symbols.Select(s => (s.Option, Value.Text(s.Symbol))).ToEquatableDictionary());
}
