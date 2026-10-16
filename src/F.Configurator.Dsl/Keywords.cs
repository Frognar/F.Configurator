namespace F.Configurator.Dsl;

internal static class Keywords
{
    private static readonly HashSet<string> All = ["reguła"];

    public static bool IsKeyword(string word) => All.Contains(word);
}
