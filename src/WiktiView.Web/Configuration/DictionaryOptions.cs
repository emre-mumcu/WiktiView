using System.ComponentModel.DataAnnotations;

namespace WiktiView.Web.Configuration;

public sealed class DictionaryOptions
{
    public const string SectionName = "Dictionary";

    /// <summary>Path of the Wiktionary JSON file. Relative paths are resolved against the content root.</summary>
    [Required]
    public string DataFilePath { get; set; } = string.Empty;

    /// <summary>Path of the binary index cache. Relative paths are resolved against the content root.</summary>
    [Required]
    public string IndexCachePath { get; set; } = "App_Data/wiktionary.idx";

    [Range(1, 500)]
    public int DefaultPageSize { get; set; } = 25;

    [Range(1, 500)]
    public int MaxPageSize { get; set; } = 100;
}
