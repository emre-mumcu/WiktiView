using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WiktiView.Web.Hosting;
using WiktiView.Web.Indexing;
using WiktiView.Web.Models;

namespace WiktiView.Web.Controllers;

public sealed class HomeController(DictionaryIndex index) : Controller
{
    [RequireIndex]
    public IActionResult Index() => View(new HomeViewModel(index.Snapshot));

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
        StatusCode = Response.StatusCode,
    });
}
