using WiktiView.Web.Search;

namespace WiktiView.Web.Presentation;

/// <summary>Turkish display texts for the codes used in the data file.</summary>
public static class Labels
{
    private static readonly Dictionary<string, string> WordTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Noun"] = "İsim",
        ["Adjective"] = "Sıfat",
        ["Verb"] = "Fiil",
        ["Expression"] = "Deyim / İfade",
        ["Adverb"] = "Zarf",
        ["Numeral"] = "Sayı",
        ["Interjection"] = "Ünlem",
        ["Preposition"] = "Edat",
        ["Pronoun"] = "Zamir",
        ["Conjunction"] = "Bağlaç",
        ["Particle"] = "Parçacık",
        ["Article"] = "Artikel",
    };

    private static readonly Dictionary<string, string> Genders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["masculine"] = "eril",
        ["feminine"] = "dişil",
        ["neuter"] = "nötr",
    };

    private static readonly Dictionary<string, string> Auxiliaries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Haben"] = "haben",
        ["Sein"] = "sein",
        ["HabenOderSein"] = "haben / sein",
    };

    private static readonly Dictionary<string, string> Tenses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Praesens"] = "Präsens",
        ["Praeteritum"] = "Präteritum",
        ["Perfekt"] = "Perfekt",
        ["FuturI"] = "Futur I",
        ["KonjunktivII"] = "Konjunktiv II",
    };

    private static readonly Dictionary<string, string> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tr"] = "Türkçe",
        ["en"] = "İngilizce",
        ["de"] = "Almanca",
    };

    /// <summary>Conjugation person keys in display order.</summary>
    public static readonly IReadOnlyList<(string Key, string Label)> Persons =
    [
        ("ich", "ich"),
        ("du", "du"),
        ("erSieEs", "er/sie/es"),
        ("wir", "wir"),
        ("ihr", "ihr"),
        ("sieSie", "sie/Sie"),
    ];

    public static readonly IReadOnlyList<string> Articles = ["der", "die", "das"];

    public static string WordType(string? value) => Lookup(WordTypes, value);

    /// <summary>Handles combined values such as "masculine / neuter".</summary>
    public static string Gender(string? value) =>
        string.IsNullOrEmpty(value)
            ? string.Empty
            : string.Join(" / ", value.Split(" / ").Select(g => Lookup(Genders, g)));

    public static string Auxiliary(string? value) => Lookup(Auxiliaries, value);
    public static string Tense(string? value) => Lookup(Tenses, value);
    public static string Language(string? value) => Lookup(Languages, value);

    public static string Field(SearchField field) => field switch
    {
        SearchField.German => "Almanca kelime",
        SearchField.Turkish => "Türkçe anlam",
        SearchField.English => "İngilizce çeviri",
        SearchField.All => "Tümü",
        _ => field.ToString(),
    };

    public static string Match(MatchMode mode) => mode switch
    {
        MatchMode.Contains => "İçerir",
        MatchMode.StartsWith => "İle başlar",
        MatchMode.Exact => "Tam eşleşme",
        _ => mode.ToString(),
    };

    public static string Sort(SortOrder order) => order switch
    {
        SortOrder.Relevance => "En uygun",
        SortOrder.Alphabetical => "Alfabetik",
        SortOrder.FileOrder => "Dosya sırası",
        _ => order.ToString(),
    };

    private static string Lookup(Dictionary<string, string> map, string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : map.GetValueOrDefault(value, value);
}
