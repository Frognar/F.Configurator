using System.Xml.Linq;

namespace F.Configurator.Architecture.Tests;

// Guards the module dependencies from section 3.1 of the analysis.
// Checks declared ProjectReferences, so it also catches references the code does not use yet.
public class ModuleDependencyTests
{
    public static TheoryData<string, string[]> AllowedDependencies => new()
    {
        { "F.Configurator.Expressions", [] },
        { "F.Configurator.Catalog", ["F.Configurator.Expressions"] },
        { "F.Configurator.Engine", ["F.Configurator.Catalog", "F.Configurator.Expressions"] },
        { "F.Configurator.StepResult", [] },
        { "F.Configurator.Pricing", ["F.Configurator.Expressions", "F.Configurator.StepResult"] },
        { "F.Configurator.Dsl", ["F.Configurator.Catalog", "F.Configurator.Pricing", "F.Configurator.Expressions"] },
    };

    [Theory]
    [MemberData(nameof(AllowedDependencies))]
    public void Module_depends_only_on_allowed_modules(string module, string[] allowed)
    {
        var references = ProjectReferencesOf(module);

        Assert.Subset(allowed.ToHashSet(), references.ToHashSet());
    }

    private static IReadOnlyList<string> ProjectReferencesOf(string module)
    {
        var path = Path.Combine(RepositoryRoot(), "src", module, $"{module}.csproj");
        Assert.True(File.Exists(path), $"Missing project for module {module}: {path}");

        return XDocument.Load(path)
            .Descendants("ProjectReference")
            // Tooling writes Windows separators in .csproj even on Linux, where Path does not split on them.
            .Select(reference => ((string)reference.Attribute("Include")!).Replace('\\', '/'))
            .Select(include => Path.GetFileNameWithoutExtension(include))
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
