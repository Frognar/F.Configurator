using System.Xml.Linq;

namespace F.Configurator.Architecture.Tests;

// Pilnuje zależności między modułami z rozdziału 3.1 analizy.
// Sprawdza zadeklarowane ProjectReference, więc łapie też referencje, których kod jeszcze nie używa.
public class ModuleDependencyTests
{
    [Fact]
    public void Expressions_does_not_depend_on_any_other_module()
    {
        var references = ProjectReferencesOf("F.Configurator.Expressions");

        Assert.Empty(references);
    }

    private static IReadOnlyList<string> ProjectReferencesOf(string module)
    {
        var path = Path.Combine(RepositoryRoot(), "src", module, $"{module}.csproj");
        Assert.True(File.Exists(path), $"Brak projektu modułu {module}: {path}");

        return XDocument.Load(path)
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension((string)reference.Attribute("Include")!))
            .ToList();
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "F.Configurator.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Nie znaleziono F.Configurator.slnx");
    }
}
