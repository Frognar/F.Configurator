using System.Xml.Linq;

namespace F.Configurator.Architecture.Tests;

// Guards the module dependencies from section 3.1 of the analysis.
// Checks declared ProjectReferences, so it also catches references the code does not use yet.
public class ModuleDependencyTests
{
    [Fact]
    public void Expressions_does_not_depend_on_any_other_module()
    {
        var references = ProjectReferencesOf("F.Configurator.Expressions");

        Assert.Empty(references);
    }

    [Fact]
    public void Catalog_depends_only_on_Expressions()
    {
        var references = ProjectReferencesOf("F.Configurator.Catalog");

        Assert.Subset(new HashSet<string> { "F.Configurator.Expressions" }, references.ToHashSet());
    }

    private static IReadOnlyList<string> ProjectReferencesOf(string module)
    {
        var path = Path.Combine(RepositoryRoot(), "src", module, $"{module}.csproj");
        Assert.True(File.Exists(path), $"Missing project for module {module}: {path}");

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
        return directory?.FullName ?? throw new InvalidOperationException("F.Configurator.slnx not found");
    }
}
