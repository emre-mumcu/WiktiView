namespace WiktiView.Web.Domain;

/// <summary>A single dictionary record exactly as stored in the Wiktionary JSON file.</summary>
public sealed class WordEntry
{
    public string Word { get; init; } = string.Empty;
    public string Lemma { get; init; } = string.Empty;
    public string WordType { get; init; } = string.Empty;
    public string? Pos { get; init; }

    // Pronunciation
    public string? Ipa { get; init; }
    public string? Hyphenation { get; init; }
    public string? Audio { get; init; }

    // Nouns
    public string? Article { get; init; }
    public string? Gender { get; init; }
    public string? Plural { get; init; }
    public string? Genitive { get; init; }

    // Adjectives
    public string? Comparative { get; init; }
    public string? Superlative { get; init; }

    // Verbs
    public string? Auxiliary { get; init; }
    public string? PartizipII { get; init; }
    public bool? IsSeparable { get; init; }
    public Dictionary<string, Dictionary<string, string>>? Conjugations { get; init; }
    public Dictionary<string, string>? Imperative { get; init; }

    // Meaning
    /// <summary>Short Turkish meaning (comma separated).</summary>
    public string? Meaning { get; init; }
    public string? Sample { get; init; }
    public List<Sense>? Senses { get; init; }
    public Dictionary<string, List<Translation>>? Translations { get; init; }
    public List<string>? Synonyms { get; init; }
    public List<string>? Antonyms { get; init; }
}

public sealed class Sense
{
    public string Index { get; init; } = string.Empty;
    public string Gloss { get; init; } = string.Empty;
    public List<string>? Tags { get; init; }
    public List<string>? Examples { get; init; }
}

public sealed class Translation
{
    public string Word { get; init; } = string.Empty;
    public string? Sense { get; init; }
}
