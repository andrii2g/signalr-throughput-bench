using System.Diagnostics;
using System.Net.Http.Json;

namespace SignalRThroughputBench.Runner.Metrics;

public sealed class ResourceSampler(HttpClient httpClient, string metricsUrl)
{
    public async Task<IReadOnlyList<ResourceRecord>> CaptureAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        var results = new List<ResourceRecord>();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < duration)
        {
            results.Add(CreateRunnerRecord(stopwatch.Elapsed));
            results.Add(await CreateServerRecordAsync(stopwatch.Elapsed, cancellationToken).ConfigureAwait(false));
            if (!await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                break;
            }
        }

        return results;
    }

    private static ResourceRecord CreateRunnerRecord(TimeSpan elapsed)
    {
        var process = Process.GetCurrentProcess();
        return new ResourceRecord(
            (int)Math.Floor(elapsed.TotalSeconds),
            "runner",
            process.WorkingSet64 / 1024d / 1024d,
            process.PrivateMemorySize64 / 1024d / 1024d,
            null,
            process.Threads.Count,
            GC.GetTotalMemory(false) / 1024d / 1024d,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));
    }

    private async Task<ResourceRecord> CreateServerRecordAsync(TimeSpan elapsed, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await httpClient.GetFromJsonAsync<ServerSnapshot>(metricsUrl, cancellationToken).ConfigureAwait(false);
            return new ResourceRecord(
                (int)Math.Floor(elapsed.TotalSeconds),
                "server",
                snapshot?.WorkingSetMb,
                snapshot?.PrivateMemoryMb,
                null,
                snapshot?.ThreadCount,
                snapshot?.GcHeapMb,
                snapshot?.Gen0,
                snapshot?.Gen1,
                snapshot?.Gen2);
        }
        catch
        {
            return new ResourceRecord((int)Math.Floor(elapsed.TotalSeconds), "server", null, null, null, null, null, null, null, null);
        }
    }

    private sealed record ServerSnapshot(
        double WorkingSetMb,
        double PrivateMemoryMb,
        int ThreadCount,
        double GcHeapMb,
        int Gen0,
        int Gen1,
        int Gen2);
}

public sealed record ResourceRecord(
    int ElapsedSecond,
    string Process,
    double? WorkingSetMb,
    double? PrivateMemoryMb,
    double? CpuPercent,
    int? ThreadCount,
    double? GcHeapMb,
    int? Gen0,
    int? Gen1,
    int? Gen2);
