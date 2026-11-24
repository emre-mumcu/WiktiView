using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using WiktiView.Web.Indexing;

namespace WiktiView.Web.Hosting;

/// <summary>
/// Short-circuits the action with a "loading" page (503 + Retry-After) while the index is still being built.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireIndexAttribute : Attribute, IActionFilter
{
    private const int RetryAfterSeconds = 3;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var index = context.HttpContext.RequestServices.GetRequiredService<DictionaryIndex>();
        if (index.IsReady)
        {
            return;
        }

        context.HttpContext.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();
        var metadataProvider = context.HttpContext.RequestServices.GetRequiredService<IModelMetadataProvider>();

        context.Result = new ViewResult
        {
            ViewName = "IndexStatus",
            StatusCode = StatusCodes.Status503ServiceUnavailable,
            ViewData = new ViewDataDictionary<DictionaryIndex>(metadataProvider, context.ModelState) { Model = index },
        };
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
