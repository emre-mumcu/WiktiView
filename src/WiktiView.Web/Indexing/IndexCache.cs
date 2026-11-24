using System.Text;
using WiktiView.Web.Domain;

namespace WiktiView.Web.Indexing;

/// <summary>
/// Persists the index to a compact binary file so the data file only has to be scanned once.
/// The cache is invalidated automatically when the data file's size or modification time changes.
/// </summary>
public sealed class IndexCache(ILogger<IndexCache> logger)
{
    private const string Magic = "WKTVIDX";
    private const int FormatVersion = 1;

    public bool TryLoad(
        string cachePath,
        FileInfo dataFile,
        out (DictionaryMetadata Metadata, List<IndexEntry> Entries) result)
    {
        result = default;

        if (!File.Exists(cachePath))
        {
            return false;
        }

        try
        {
            using var stream = File.OpenRead(cachePath);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            if (reader.ReadString() != Magic
                || reader.ReadInt32() != FormatVersion
                || reader.ReadInt64() != dataFile.Length
                || reader.ReadInt64() != dataFile.LastWriteTimeUtc.Ticks)
            {
                logger.LogInformation("Index cache {Path} is outdated and will be rebuilt.", cachePath);
                return false;
            }

            var metadata = new DictionaryMetadata(
                ReadNullable(reader), ReadNullable(reader), ReadNullable(reader),
                reader.ReadBoolean() ? reader.ReadInt32() : null);

            var count = reader.ReadInt32();
            var entries = new List<IndexEntry>(count);

            for (var id = 0; id < count; id++)
            {
                entries.Add(new IndexEntry(
                    id,
                    offset: reader.ReadInt64(),
                    length: reader.ReadInt32(),
                    word: reader.ReadString(),
                    lemma: reader.ReadString(),
                    wordType: reader.ReadString(),
                    article: ReadNullable(reader),
                    turkish: ReadArray(reader),
                    english: ReadArray(reader)));
            }

            result = (metadata, entries);
            return true;
        }
        catch (Exception ex) when (ex is IOException or EndOfStreamException or InvalidDataException or FormatException)
        {
            logger.LogWarning(ex, "Index cache {Path} could not be read and will be rebuilt.", cachePath);
            return false;
        }
    }

    public void Save(string cachePath, FileInfo dataFile, DictionaryMetadata metadata, IReadOnlyList<IndexEntry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(cachePath))!);
        var tempPath = cachePath + ".tmp";

        using (var stream = File.Create(tempPath))
        using (var writer = new BinaryWriter(stream, Encoding.UTF8))
        {
            writer.Write(Magic);
            writer.Write(FormatVersion);
            writer.Write(dataFile.Length);
            writer.Write(dataFile.LastWriteTimeUtc.Ticks);

            WriteNullable(writer, metadata.Source);
            WriteNullable(writer, metadata.License);
            WriteNullable(writer, metadata.GeneratedAt);
            writer.Write(metadata.DeclaredCount.HasValue);
            if (metadata.DeclaredCount.HasValue)
            {
                writer.Write(metadata.DeclaredCount.Value);
            }

            writer.Write(entries.Count);
            foreach (var entry in entries)
            {
                writer.Write(entry.Offset);
                writer.Write(entry.Length);
                writer.Write(entry.Word);
                writer.Write(entry.Lemma);
                writer.Write(entry.WordType);
                WriteNullable(writer, entry.Article);
                WriteArray(writer, entry.Turkish);
                WriteArray(writer, entry.English);
            }
        }

        File.Move(tempPath, cachePath, overwrite: true);
    }

    private static string? ReadNullable(BinaryReader reader) => reader.ReadBoolean() ? reader.ReadString() : null;

    private static void WriteNullable(BinaryWriter writer, string? value)
    {
        writer.Write(value is not null);
        if (value is not null)
        {
            writer.Write(value);
        }
    }

    private static string[] ReadArray(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        if (length == 0)
        {
            return [];
        }

        var items = new string[length];
        for (var i = 0; i < length; i++)
        {
            items[i] = reader.ReadString();
        }

        return items;
    }

    private static void WriteArray(BinaryWriter writer, string[] items)
    {
        writer.Write(items.Length);
        foreach (var item in items)
        {
            writer.Write(item);
        }
    }
}
