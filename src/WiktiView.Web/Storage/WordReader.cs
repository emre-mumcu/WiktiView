using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;
using WiktiView.Web.Configuration;
using WiktiView.Web.Domain;
using WiktiView.Web.Indexing;

namespace WiktiView.Web.Storage;

/// <summary>
/// Reads individual records from the data file using their byte offsets. Only the requested bytes are read;
/// <see cref="RandomAccess"/> is thread-safe, so a single shared handle serves all requests.
/// </summary>
public sealed class WordReader : IDisposable
{
    private readonly Lazy<SafeFileHandle> _handle;

    public WordReader(IOptions<DictionaryOptions> options)
    {
        var path = options.Value.DataFilePath;
        _handle = new Lazy<SafeFileHandle>(
            () => File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.RandomAccess));
    }

    public WordEntry Read(IndexEntry entry) => Read(entry, bytes =>
        JsonSerializer.Deserialize<WordEntry>(bytes, JsonDefaults.Options)
        ?? throw new InvalidDataException($"Record #{entry.Id} is empty."));

    public IReadOnlyList<WordEntry> ReadMany(IEnumerable<IndexEntry> entries) => entries.Select(Read).ToList();

    /// <summary>Returns the record as indented JSON, exactly as it is stored (formatting aside).</summary>
    public string ReadRawJson(IndexEntry entry) => Read(entry, bytes =>
    {
        using var document = JsonDocument.Parse(bytes.ToArray());
        return JsonSerializer.Serialize(document.RootElement, JsonDefaults.Pretty);
    });

    private T Read<T>(IndexEntry entry, Func<ReadOnlySpan<byte>, T> parse)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(entry.Length);
        try
        {
            var span = buffer.AsSpan(0, entry.Length);
            var total = 0;
            while (total < span.Length)
            {
                var read = RandomAccess.Read(_handle.Value, span[total..], entry.Offset + total);
                if (read == 0)
                {
                    throw new InvalidDataException(
                        $"Unexpected end of file while reading record #{entry.Id}. The data file may have changed; restart the application.");
                }

                total += read;
            }

            return parse(span);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                $"Record #{entry.Id} could not be parsed. The data file may have changed; restart the application.", ex);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        if (_handle.IsValueCreated)
        {
            _handle.Value.Dispose();
        }
    }
}
