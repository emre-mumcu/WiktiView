using Microsoft.Extensions.Options;
using WiktiView.Web.Configuration;
using WiktiView.Web.Indexing;

namespace WiktiView.Web.Search;

/// <summary>Filters, ranks and pages the in-memory index. Never touches the data file.</summary>
public sealed class WordSearchService(DictionaryIndex index, IOptions<DictionaryOptions> options)
{
    private const int NoMatch = int.MaxValue;

    public PagedResult<IndexEntry> Search(SearchCriteria criteria)
    {
        var snapshot = index.Snapshot;
        var pageSize = Math.Clamp(criteria.PageSize ?? options.Value.DefaultPageSize, 1, options.Value.MaxPageSize);
        var key = TextNormalizer.Normalize(criteria.Q);

        IEnumerable<int> order = criteria.Sort == SortOrder.FileOrder
            ? Enumerable.Range(0, snapshot.Entries.Count)
            : snapshot.AlphabeticalOrder;

        var candidates = order
            .Select(id => snapshot.Entries[id])
            .Where(entry => MatchesFilters(entry, criteria));

        List<IndexEntry> matches;
        if (key.Length == 0)
        {
            matches = candidates.ToList();
        }
        else
        {
            var scored = candidates
                .Select(entry => (Entry: entry, Score: Score(entry, key, criteria.Field, criteria.Match)))
                .Where(x => x.Score != NoMatch);

            if (criteria.Sort == SortOrder.Relevance)
            {
                scored = scored.OrderBy(x => x.Score); // stable: keeps alphabetical order inside each score
            }

            matches = scored.Select(x => x.Entry).ToList();
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(matches.Count / (double)pageSize));
        var page = Math.Clamp(criteria.Page, 1, totalPages);
        var items = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<IndexEntry>(items, page, pageSize, matches.Count);
    }

    /// <summary>Entries whose lemma (or full word, e.g. "das Haus") equals the given text.</summary>
    public IReadOnlyList<IndexEntry> FindExact(string text)
    {
        var key = TextNormalizer.Normalize(text);
        return index.Snapshot.Entries
            .Where(e => e.LemmaKey == key || e.WordKey == key)
            .ToList();
    }

    private static bool MatchesFilters(IndexEntry entry, SearchCriteria criteria)
    {
        if (!string.IsNullOrEmpty(criteria.WordType) && entry.WordType != criteria.WordType)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(criteria.Article)
            && (entry.Article is null || !entry.Article.Split(" / ").Contains(criteria.Article)))
        {
            return false;
        }

        return !criteria.OnlyTurkish || entry.Turkish.Length > 0;
    }

    /// <returns>0 = exact, 1 = prefix, 2 = contains, <see cref="NoMatch"/> otherwise.</returns>
    private static int Score(IndexEntry entry, string key, SearchField field, MatchMode mode)
    {
        var best = NoMatch;

        if (field is SearchField.German or SearchField.All)
        {
            best = Math.Min(best, Score(entry.LemmaKey, key, mode));
            if (mode == MatchMode.Exact && entry.WordKey == key)
            {
                best = 0;
            }
        }

        if (field is SearchField.Turkish or SearchField.All)
        {
            best = Math.Min(best, Score(entry.TurkishKeys, key, mode));
        }

        if (field is SearchField.English or SearchField.All)
        {
            best = Math.Min(best, Score(entry.EnglishKeys, key, mode));
        }

        return best;
    }

    private static int Score(string[] values, string key, MatchMode mode)
    {
        var best = NoMatch;
        foreach (var value in values)
        {
            best = Math.Min(best, Score(value, key, mode));
            if (best == 0)
            {
                break;
            }
        }

        return best;
    }

    private static int Score(string value, string key, MatchMode mode)
    {
        if (value == key)
        {
            return 0;
        }

        if (mode == MatchMode.Exact)
        {
            return NoMatch;
        }

        if (value.StartsWith(key, StringComparison.Ordinal))
        {
            return 1;
        }

        return mode == MatchMode.Contains && value.Contains(key, StringComparison.Ordinal) ? 2 : NoMatch;
    }
}
