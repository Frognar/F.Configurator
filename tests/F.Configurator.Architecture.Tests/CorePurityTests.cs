using System.Text.RegularExpressions;

namespace F.Configurator.Architecture.Tests;

// Guards the core rules from section 3.1 of the analysis: no I/O, no ambient clock, no ambient culture.
// Scans source text, so it is a tripwire for the obvious cases, not a proof.
public class CorePurityTests
{
    public static TheoryData<string> CoreModules => new()
    {
        "F.Configurator.Expressions",
        "F.Configurator.Catalog",
        "F.Configurator.Engine",
        "F.Configurator.StepResult",
        "F.Configurator.Pricing",
    };

    private static readonly Regex ForbiddenApi = new(
        @"\b(DateTime(Offset)?\.(Now|UtcNow|Today)|CultureInfo\.Current(UI)?Culture|File\.|Directory\.|Console\.)",
        RegexOptions.Compiled);

    [Theory]
    [MemberData(nameof(CoreModules))]
    public void Core_module_does_not_use_ambient_clock_culture_or_io(string module)
    {
        var violations = Directory
            .EnumerateFiles(Repository.ModuleDirectory(module), "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsBuildOutput(path))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => (path, line, number: index + 1)))
            .Where(entry => ForbiddenApi.IsMatch(entry.line))
            .Select(entry => $"{entry.path}:{entry.number}: {entry.line.Trim()}")
            .ToList();

        Assert.Empty(violations);
    }

    private static bool IsBuildOutput(string path) =>
        path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj");
}
