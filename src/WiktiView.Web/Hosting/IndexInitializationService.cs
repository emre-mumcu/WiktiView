using System.Diagnostics;
using Microsoft.Extensions.Options;
using WiktiView.Web.Configuration;
using WiktiView.Web.Indexing;

namespace WiktiView.Web.Hosting;

/// <summary>
/// Loads the index from the cache, or builds it by streaming the data file, without blocking application startup.
/// </summary>
public sealed class IndexInitializationService(
    DictionaryIndex index,
    IndexCache cache,
    IOptions<DictionaryOptions> options,
    ILogger<IndexInitializationService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => Initialize(stoppingToken), stoppingToken);

    private void Initialize(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var dataFile = new FileInfo(settings.DataFilePath);

        try
        {
            if (!dataFile.Exists)
            {
                throw new FileNotFoundException($"Data file not found: {dataFile.FullName}");
            }

            var stopwatch = Stopwatch.StartNew();
            index.ReportProgress(0);

            if (!cache.TryLoad(settings.IndexCachePath, dataFile, out var data))
            {
                logger.LogInformation("Building index from {DataFile}...", dataFile.FullName);
                data = IndexBuilder.Build(dataFile.FullName, index.ReportProgress, cancellationToken);
                cache.Save(settings.IndexCachePath, dataFile, data.Metadata, data.Entries);
            }

            index.Complete(new IndexSnapshot(data.Metadata, data.Entries));

            if (data.Metadata.DeclaredCount is { } declared && declared != data.Entries.Count)
            {
                logger.LogWarning(
                    "Data file declares {Declared} records but {Actual} were indexed.", declared, data.Entries.Count);
            }

            logger.LogInformation(
                "Dictionary index ready: {Count} records in {Elapsed} ms.", data.Entries.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Application is shutting down.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dictionary index could not be built.");
            index.Fail(ex.Message);
        }
    }
}
