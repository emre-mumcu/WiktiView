using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace WiktiView.Web.Domain;

public static class JsonDefaults
{
    /// <summary>camelCase, case insensitive options matching the data file.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Indented output for displaying raw records (keeps umlauts readable).</summary>
    public static readonly JsonSerializerOptions Pretty = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };
}
