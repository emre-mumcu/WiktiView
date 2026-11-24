namespace WiktiView.Web.Indexing;

public enum IndexState
{
    Pending,
    Building,
    Ready,
    Failed,
}

/// <summary>
/// Thread-safe holder of the current index. The index is built in the background at startup,
/// so consumers must check <see cref="IsReady"/> before using <see cref="Snapshot"/>.
/// </summary>
public sealed class DictionaryIndex
{
    private volatile IndexSnapshot? _snapshot;
    private volatile IndexState _state = IndexState.Pending;
    private volatile string? _error;
    private double _progress;

    public IndexState State => _state;
    public bool IsReady => _state == IndexState.Ready;
    public string? Error => _error;

    /// <summary>Build progress between 0 and 1.</summary>
    public double Progress => Volatile.Read(ref _progress);

    public IndexSnapshot Snapshot =>
        _snapshot ?? throw new InvalidOperationException("The dictionary index is not ready yet.");

    internal void ReportProgress(double progress)
    {
        _state = IndexState.Building;
        Volatile.Write(ref _progress, Math.Clamp(progress, 0, 1));
    }

    internal void Complete(IndexSnapshot snapshot)
    {
        _snapshot = snapshot;
        Volatile.Write(ref _progress, 1);
        _state = IndexState.Ready;
    }

    internal void Fail(string error)
    {
        _error = error;
        _state = IndexState.Failed;
    }
}
