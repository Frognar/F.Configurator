namespace F.Configurator.Dsl;

public static class Compiler
{
    // The files of a package by their paths in the package folder: `.kfg` sources and the data files they name.
    public static Compilation Compile(IReadOnlyDictionary<string, string> files) => new PackageCompiler(files).Compile();
}
