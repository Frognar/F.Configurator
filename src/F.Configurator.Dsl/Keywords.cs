namespace F.Configurator.Dsl;

internal static class Keywords
{
    // Grammar 3.4, the words of the EBNF, G15 and G18, and the built-in functions.
    private static readonly string[] WithPolishLetters =
    [
        "katalog", "cecha", "opcja", "atrybut", "wyliczana", "tabela", "kolekcja", "etap", "komponent", "kontekst",
        "reguła", "powiązanie", "indeks", "wizualizacja", "warstwa", "test", "cennik", "dopłata", "przypisz",
        "nazwa", "opis", "prezentacja", "jednostka", "wymagana", "opcje", "atrybuty", "symbol", "obraz", "z", "klucz",
        "wartości", "domyślnie", "min", "maks", "krok", "długość", "znaki", "wzorzec", "reakcja", "tryb", "cechy",
        "wspólne", "własne", "priorytet", "dotyczy", "gdy", "wtedy", "przy", "naruszeniu",
        "ukryj", "zablokuj", "tylko", "zabroń", "ustaw", "koryguj", "błąd", "ostrzeż", "informuj",
        "i", "lub", "nie", "w", "jeśli", "to", "inaczej",
        "do", "proponuj", "przejmij", "jako",
        "waluta", "baza", "kwota", "rozszerza", "wyłącz", "nadpisz", "rabat", "narzut", "zaokrąglij", "kontrahenta",
        "kontrahentów",
        "dane", "oczekuj", "zawiera", "niedostępne", "komunikat",
        "wybór", "wielokrotny", "liczba", "tekst", "tak/nie", "kolejny", "niezależny", "widoczna", "wszystkich",
        "domyślne", "zablokowane", "domyślny", "dozwolone", "tekstura", "odbij", "cena", "składnik", "ukryta",
        "domyślna", "wybrana", "dopełnij",
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
