using WiktiView.Web.Search;

namespace WiktiView.Web.Indexing;

/// <summary>
/// Lightweight, in-memory pointer to a record in the data file. It only holds what is needed for
/// searching and filtering; the full record is always read from disk on demand.
/// </summary>
public sealed class IndexEntry
{
    public IndexEntry(
        int id,
        long offset,
        int length,
        string word,
        string lemma,
        string wordType,
        string? article,
        string[] turkish,
        string[] english)
    {
        Id = id;
        Offset = offset;
        Length = length;
        Word = word;
        Lemma = lemma;
        WordType = wordType;
        Article = article;
        Turkish = turkish;
        English = english;

        LemmaKey = TextNormalizer.Normalize(lemma);
        WordKey = TextNormalizer.Normalize(word);
        TurkishKeys = Array.ConvertAll(turkish, TextNormalizer.Normalize);
        EnglishKeys = Array.ConvertAll(english, TextNormalizer.Normalize);
    }

    /// <summary>Zero based position of the record inside the "words" array.</summary>
    public int Id { get; }

    /// <summary>Byte offset of the record inside the data file.</summary>
    public long Offset { get; }

    /// <summary>Length of the record in bytes.</summary>
    public int Length { get; }

    public string Word { get; }
    public string Lemma { get; }
    public string WordType { get; }
    public string? Article { get; }
    public string[] Turkish { get; }
    public string[] English { get; }

    public string LemmaKey { get; }
    public string WordKey { get; }
    public string[] TurkishKeys { get; }
    public string[] EnglishKeys { get; }
}
