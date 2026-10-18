using F.Configurator.Catalog;

namespace F.Configurator.Dsl;

// No catalog when any diagnostic is an error.
public sealed record Compilation(ProductCatalog? Catalog, IReadOnlyList<Diagnostic> Diagnostics);
