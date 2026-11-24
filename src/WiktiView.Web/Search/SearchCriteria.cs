namespace WiktiView.Web.Search;

public enum SearchField
{
    German,
    Turkish,
    English,
    All,
}

public enum MatchMode
{
    Contains,
    StartsWith,
    Exact,
}

public enum SortOrder
{
    /// <summary>Exact matches first, then prefix matches, then the rest; alphabetical within each group.</summary>
    Relevance,
    Alphabetical,
    FileOrder,
}

/// <summary>Search parameters, bound from the query string.</summary>
public sealed class SearchCriteria
{
    public string? Q { get; set; }
    public SearchField Field { get; set; } = SearchField.German;
    public MatchMode Match { get; set; } = MatchMode.Contains;
    public string? WordType { get; set; }

    /// <summary>der, die or das; only meaningful for nouns.</summary>
    public string? Article { get; set; }

    public bool OnlyTurkish { get; set; }
    public SortOrder Sort { get; set; } = SortOrder.Relevance;
    public int Page { get; set; } = 1;
    public int? PageSize { get; set; }

    public bool HasQuery => !string.IsNullOrWhiteSpace(Q);

    public bool IsEmpty =>
        !HasQuery && string.IsNullOrEmpty(WordType) && string.IsNullOrEmpty(Article) && !OnlyTurkish;

    /// <summary>Route values (defaults omitted) for building links to another page of the same search.</summary>
    public Dictionary<string, string> ToRouteValues(int page)
    {
        var values = new Dictionary<string, string?>
        {
            [nameof(Q)] = Q,
            [nameof(Field)] = Field == SearchField.German ? null : Field.ToString(),
            [nameof(Match)] = Match == MatchMode.Contains ? null : Match.ToString(),
            [nameof(WordType)] = WordType,
            [nameof(Article)] = Article,
            [nameof(OnlyTurkish)] = OnlyTurkish ? "true" : null,
            [nameof(Sort)] = Sort == SortOrder.Relevance ? null : Sort.ToString(),
            [nameof(PageSize)] = PageSize?.ToString(),
            [nameof(Page)] = page > 1 ? page.ToString() : null,
        };

        return values
            .Where(pair => !string.IsNullOrEmpty(pair.Value))
            .ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value!);
    }
}
