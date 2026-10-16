namespace F.Configurator.Dsl;

internal static class Keywords
{
    private static readonly string[] WithPolishLetters =
    [
        "reguła", "wybór", "błąd", "jeśli", "domyślnie", "długość", "zabroń", "wartości", "ostrzeż", "niezależny",
        "kontrahentów",
    ];

    // Each keyword under its own spelling and under the spelling without Polish letters.
    private static readonly Dictionary<string, string> BySpelling = WithPolishLetters
        .SelectMany(keyword => new[] { (Written: keyword, Keyword: keyword), (Written: WithoutPolishLetters(keyword), Keyword: keyword) })
        .DistinctBy(x => x.Written)
        .ToDictionary(x => x.Written, x => x.Keyword);

    public static string? KeywordOf(string word) => BySpelling.GetValueOrDefault(word);

    private static string WithoutPolishLetters(string word) =>
        string.Concat(word.Select(letter => letter switch
        {
            'ą' => 'a',
            'ć' => 'c',
            'ę' => 'e',
            'ł' => 'l',
            'ń' => 'n',
            'ó' => 'o',
            'ś' => 's',
            'ź' or 'ż' => 'z',
            _ => letter,
        }));
}
