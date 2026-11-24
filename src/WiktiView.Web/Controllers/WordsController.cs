using Microsoft.AspNetCore.Mvc;
using WiktiView.Web.Hosting;
using WiktiView.Web.Indexing;
using WiktiView.Web.Models;
using WiktiView.Web.Search;
using WiktiView.Web.Storage;

namespace WiktiView.Web.Controllers;

[RequireIndex]
public sealed class WordsController(
    DictionaryIndex index,
    WordSearchService searchService,
    WordReader reader) : Controller
{
    /// <summary>Paged search. Only the records on the requested page are read from the data file.</summary>
    [HttpGet]
    public IActionResult Index([FromQuery] SearchCriteria criteria)
    {
        var page = searchService.Search(criteria);
        var results = page.Map(entry => new WordListItem(entry, reader.Read(entry)));

        return View(new WordListViewModel(criteria, results, index.Snapshot.WordTypeCounts));
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var snapshot = index.Snapshot;
        var entry = snapshot.Find(id);
        if (entry is null)
        {
            return NotFound();
        }

        return View(new WordDetailsViewModel(
            entry,
            reader.Read(entry),
            reader.ReadRawJson(entry),
            snapshot.Neighbour(entry, -1),
            snapshot.Neighbour(entry, +1)));
    }

    /// <summary>Opens the word directly when the term is unique, otherwise lists all exact matches.</summary>
    [HttpGet]
    public IActionResult Lookup(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return RedirectToAction(nameof(Index));
        }

        var matches = searchService.FindExact(term);
        return matches.Count == 1
            ? RedirectToAction(nameof(Details), new { id = matches[0].Id })
            : RedirectToAction(nameof(Index), new { q = term, match = MatchMode.Exact });
    }

    [HttpGet]
    public IActionResult Random()
    {
        var entries = index.Snapshot.Entries;
        if (entries.Count == 0)
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Details), new { id = System.Random.Shared.Next(entries.Count) });
    }

    /// <summary>The raw record as JSON.</summary>
    [HttpGet]
    public IActionResult Json(int id)
    {
        var entry = index.Snapshot.Find(id);
        return entry is null ? NotFound() : Content(reader.ReadRawJson(entry), "application/json; charset=utf-8");
    }
}
