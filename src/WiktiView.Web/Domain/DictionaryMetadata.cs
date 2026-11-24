namespace WiktiView.Web.Domain;

/// <summary>Top level (non record) properties of the data file.</summary>
public sealed record DictionaryMetadata(string? Source, string? License, string? GeneratedAt, int? DeclaredCount);
