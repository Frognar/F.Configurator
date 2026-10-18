using F.Configurator.Catalog;

namespace F.Configurator.Dsl.Tests;

// A package is its files by path, as `fkfg` reads them from the package folder.
internal static class Compiling
{
    public const string CatalogFile = "katalog.kfg";

    public const string LibraryFile = "biblioteka/cechy.kfg";

    public static Compilation Compile(params (string Path, string Text)[] files) =>
        Compiler.Compile(files.ToDictionary(file => file.Path, file => file.Text));

    // The catalog `Demo` with one library file and its data files.
    public static Compilation CompileLibrary(string library, params (string Path, string Text)[] data) =>
        Compile([(CatalogFile, "katalog \"Demo\""), (LibraryFile, library), .. data]);

    public static ProductCatalog Library(string library, params (string Path, string Text)[] data)
    {
        var compilation = CompileLibrary(library, data);
        Assert.Empty(compilation.Diagnostics);
        return compilation.Catalog!;
    }

    public static IReadOnlyList<Diagnostic> LibraryDiagnostics(string library, params (string Path, string Text)[] data) =>
        CompileLibrary(library, data).Diagnostics;

    public static SourceSpan In(string file, int line, int column, int length) => new(file, line, column, length);

    public static Diagnostic Error(SourceSpan span, string message) => new(Severity.Error, span, message, null);

    public static Diagnostic Warning(SourceSpan span, string message) => new(Severity.Warning, span, message, null);
}
