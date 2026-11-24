using System.Globalization;
using WiktiView.Web.Domain;

namespace WiktiView.Web.Indexing;

/// <summary>Immutable, fully built index of the data file.</summary>
public sealed class IndexSnapshot
{
    private static readonly CompareInfo GermanCompare = CultureInfo.GetCultureInfo("de-DE").CompareInfo;

    public IndexSnapshot(DictionaryMetadata metadata, IReadOnlyList<IndexEntry> entries)
    {
        Metadata = metadata;
        Entries = entries;

        AlphabeticalOrder = Enumerable.Range(0, entries.Count).ToArray();
        Array.Sort(AlphabeticalOrder, (a, b) =>
        {
            var result = GermanCompare.Compare(entries[a].Lemma, entries[b].Lemma, CompareOptions.IgnoreCase);
            return result != 0 ? result : a.CompareTo(b);
        });

        AlphabeticalPosition = new int[entries.Count];
        for (var position = 0; position < AlphabeticalOrder.Length; position++)
        {
            AlphabeticalPosition[AlphabeticalOrder[position]] = position;
        }

        WordTypeCounts = entries
            .GroupBy(e => e.WordType)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        TurkishCount = entries.Count(e => e.Turkish.Length > 0);
        EnglishCount = entries.Count(e => e.English.Length > 0);
    }

    public DictionaryMetadata Metadata { get; }

    /// <summary>Entries in file order; the list index equals <see cref="IndexEntry.Id"/>.</summary>
    public IReadOnlyList<IndexEntry> Entries { get; }

    /// <summary>Entry ids sorted by lemma using German collation.</summary>
    public int[] AlphabeticalOrder { get; }

    public IReadOnlyDictionary<string, int> WordTypeCounts { get; }
    public int TurkishCount { get; }
    public int EnglishCount { get; }

    /// <summary>Inverse of <see cref="AlphabeticalOrder"/>: entry id → alphabetical position.</summary>
    public int[] AlphabeticalPosition { get; }

    public IndexEntry? Find(int id) => id >= 0 && id < Entries.Count ? Entries[id] : null;

    /// <summary>Alphabetical neighbour of an entry (offset -1 = previous, +1 = next).</summary>
    public IndexEntry? Neighbour(IndexEntry entry, int offset)
    {
        var position = AlphabeticalPosition[entry.Id] + offset;
        return position >= 0 && position < AlphabeticalOrder.Length ? Entries[AlphabeticalOrder[position]] : null;
    }
}
