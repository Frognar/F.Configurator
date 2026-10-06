namespace F.Configurator.Architecture.Tests;

internal static class Repository
{
    public static string Root { get; } = FindRoot();

    public static string ModuleDirectory(string module) => Path.Combine(Root, "src", module);

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "F.Configurator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("F.Configurator.slnx not found");
    }
}
