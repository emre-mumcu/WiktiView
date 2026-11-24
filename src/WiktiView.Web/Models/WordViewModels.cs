using WiktiView.Web.Domain;
using WiktiView.Web.Indexing;
using WiktiView.Web.Search;

namespace WiktiView.Web.Models;

public sealed record WordListItem(IndexEntry Entry, WordEntry Word);

public sealed record WordListViewModel(
    SearchCriteria Criteria,
    PagedResult<WordListItem> Results,
    IReadOnlyDictionary<string, int> WordTypes);

public sealed record WordDetailsViewModel(
    IndexEntry Entry,
    WordEntry Word,
    string RawJson,
    IndexEntry? Previous,
    IndexEntry? Next);

public sealed record HomeViewModel(IndexSnapshot Snapshot);

public sealed record SearchFormViewModel(SearchCriteria Criteria, IReadOnlyDictionary<string, int> WordTypes);
