namespace WiktiView.Web.Models;

public sealed class ErrorViewModel
{
    public string? RequestId { get; init; }
    public int StatusCode { get; init; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public bool IsNotFound => StatusCode == StatusCodes.Status404NotFound;
}
