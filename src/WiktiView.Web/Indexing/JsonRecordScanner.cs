using System.Buffers;
using System.Text.Json;

namespace WiktiView.Web.Indexing;

public delegate void RecordHandler(long offset, ReadOnlySpan<byte> json);

/// <summary>
/// Streams through the data file chunk by chunk with <see cref="Utf8JsonReader"/> and reports the exact byte
/// range of every object inside the top level "words" array. The file is never loaded into memory as a whole and
/// the scanner does not depend on line breaks or any other formatting.
/// </summary>
public static class JsonRecordScanner
{
    private const string RecordsPropertyName = "words";
    private const int InitialBufferSize = 1024 * 1024;

    /// <returns>Scalar top level properties (e.g. source, license, count).</returns>
    public static IReadOnlyDictionary<string, string> Scan(
        Stream stream,
        RecordHandler onRecord,
        Action<long>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var header = new Dictionary<string, string>(StringComparer.Ordinal);
        var buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);

        try
        {
            var state = new JsonReaderState();
            long bufferFileOffset = 0; // file offset of buffer[0]
            var dataLength = 0;        // valid bytes in buffer
            var consumed = 0;          // bytes of buffer already processed by the reader
            var recordStart = -1;      // buffer index where the current record starts
            string? currentProperty = null;
            var insideRecords = false;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var read = stream.Read(buffer, dataLength, buffer.Length - dataLength);
                dataLength += read;
                var isFinalBlock = read == 0;

                var reader = new Utf8JsonReader(buffer.AsSpan(consumed, dataLength - consumed), isFinalBlock, state);

                while (reader.Read())
                {
                    var tokenStart = consumed + (int)reader.TokenStartIndex;

                    if (insideRecords)
                    {
                        if (reader.CurrentDepth == 2 && reader.TokenType == JsonTokenType.StartObject)
                        {
                            recordStart = tokenStart;
                        }
                        else if (reader.CurrentDepth == 2 && reader.TokenType == JsonTokenType.EndObject && recordStart >= 0)
                        {
                            var recordEnd = tokenStart + 1;
                            onRecord(bufferFileOffset + recordStart, buffer.AsSpan(recordStart, recordEnd - recordStart));
                            recordStart = -1;
                        }
                        else if (reader.CurrentDepth == 1 && reader.TokenType == JsonTokenType.EndArray)
                        {
                            insideRecords = false;
                        }

                        continue;
                    }

                    if (reader.CurrentDepth != 1)
                    {
                        continue;
                    }

                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            currentProperty = reader.GetString();
                            break;
                        case JsonTokenType.StartArray when currentProperty == RecordsPropertyName:
                            insideRecords = true;
                            break;
                        case JsonTokenType.String when currentProperty is not null:
                            header[currentProperty] = reader.GetString()!;
                            break;
                        case JsonTokenType.Number or JsonTokenType.True or JsonTokenType.False when currentProperty is not null:
                            header[currentProperty] = System.Text.Encoding.UTF8.GetString(reader.ValueSpan);
                            break;
                        // Nested header structures other than "words" are ignored by the depth check above.
                    }
                }

                consumed += (int)reader.BytesConsumed;
                state = reader.CurrentState;
                onProgress?.Invoke(bufferFileOffset + consumed);

                if (isFinalBlock)
                {
                    break;
                }

                // Keep the unfinished record (or the unconsumed tail) and move it to the start of the buffer.
                var keepFrom = recordStart >= 0 ? recordStart : consumed;
                var remaining = dataLength - keepFrom;
                Buffer.BlockCopy(buffer, keepFrom, buffer, 0, remaining);
                bufferFileOffset += keepFrom;
                dataLength = remaining;
                consumed -= keepFrom;
                if (recordStart >= 0)
                {
                    recordStart = 0;
                }

                if (dataLength == buffer.Length)
                {
                    // A single record is larger than the buffer: grow it.
                    var larger = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    Buffer.BlockCopy(buffer, 0, larger, 0, dataLength);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = larger;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return header;
    }
}
