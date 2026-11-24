using System.Text.Json;
using WiktiView.Web.Domain;

namespace WiktiView.Web.Indexing;

/// <summary>Builds the in-memory index by streaming the data file once.</summary>
public static class IndexBuilder
{
    private const int ReadBufferSize = 1024 * 1024;

    public static (DictionaryMetadata Metadata, List<IndexEntry> Entries) Build(
        string dataFilePath,
        Action<double>? onProgress,
        CancellationToken cancellationToken)
    {
        using var stream = new FileStream(
            dataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, ReadBufferSize, FileOptions.SequentialScan);

        var totalBytes = Math.Max(1, stream.Length);
        var entries = new List<IndexEntry>(capacity: 200_000);

        var header = JsonRecordScanner.Scan(
            stream,
            (offset, json) => entries.Add(CreateEntry(entries.Count, offset, json)),
            position => onProgress?.Invoke((double)position / totalBytes),
            cancellationToken);

        return (CreateMetadata(header), entries);
    }

    private static IndexEntry CreateEntry(int id, long offset, ReadOnlySpan<byte> json)
    {
        var record = JsonSerializer.Deserialize<IndexProjection>(json, JsonDefaults.Options)
                     ?? throw new InvalidDataException($"Record #{id} at offset {offset} is empty.");

        var lemma = record.Lemma ?? record.Word ?? string.Empty;

        return new IndexEntry(
            id,
            offset,
            json.Length,
            word: record.Word ?? lemma,
            lemma: lemma,
            wordType: string.IsNullOrWhiteSpace(record.WordType) ? "Unknown" : record.WordType,
            article: record.Article,
            turkish: CollectTurkish(record),
            english: CollectTranslations(record, "en").ToArray());
    }

    private static string[] CollectTurkish(IndexProjection record)
    {
        var fromMeaning = (record.Meaning ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return fromMeaning
            .Concat(CollectTranslations(record, "tr"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> CollectTranslations(IndexProjection record, string language) =>
        record.Translations is not null && record.Translations.TryGetValue(language, out var items)
            ? items.Select(t => t.Word.Trim())
                .Where(w => w.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
            : [];

    private static DictionaryMetadata CreateMetadata(IReadOnlyDictionary<string, string> header) =>
        new(
            header.GetValueOrDefault("source"),
            header.GetValueOrDefault("license"),
            header.GetValueOrDefault("generatedAt"),
            int.TryParse(header.GetValueOrDefault("count"), out var count) ? count : null);

    /// <summary>Only the properties needed for the index; everything else is skipped by the serializer.</summary>
    private sealed class IndexProjection
    {
        public string? Word { get; init; }
        public string? Lemma { get; init; }
        public string? WordType { get; init; }
        public string? Article { get; init; }
        public string? Meaning { get; init; }
        public Dictionary<string, List<Translation>>? Translations { get; init; }
    }
}
