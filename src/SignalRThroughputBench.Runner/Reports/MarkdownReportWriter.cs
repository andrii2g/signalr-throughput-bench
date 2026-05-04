using SignalRThroughputBench.Contracts.Reports;

namespace SignalRThroughputBench.Runner.Reports;

public static class MarkdownReportWriter
{
    public static async Task WriteAsync(string path, RunSummary summary, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var writer = new StreamWriter(path);
        await writer.WriteLineAsync($"# Benchmark Report: {summary.RunId}").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
        await writer.WriteLineAsync("## Run Configuration").ConfigureAwait(false);
        await writer.WriteLineAsync($"- Scenario: `{summary.Scenario}`").ConfigureAwait(false);
        await writer.WriteLineAsync($"- Protocol: `{summary.Protocol}`").ConfigureAwait(false);
        await writer.WriteLineAsync($"- Transport requested: `{summary.TransportRequested}`").ConfigureAwait(false);
        await writer.WriteLineAsync($"- Transport observed: `{summary.TransportObserved}`").ConfigureAwait(false);
        await writer.WriteLineAsync($"- Connections: `{summary.Connections}`").ConfigureAwait(false);
        await writer.WriteLineAsync($"- Payload bytes: `{summary.PayloadBytes}`").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
        await writer.WriteLineAsync("## Summary").ConfigureAwait(false);
        await writer.WriteLineAsync($"- Total operations: `{summary.TotalOperations}`").ConfigureAwait(false);
        await writer.WriteLineAsync($"- Failed operations: `{summary.FailedOperations}`").ConfigureAwait(false);
        await writer.WriteLineAsync($"- Operations per second: `{summary.OperationsPerSecond:F3}`").ConfigureAwait(false);
        await writer.WriteLineAsync($"- Latency p50/p95/p99: `{summary.Latency.P50Ms:F3}` / `{summary.Latency.P95Ms:F3}` / `{summary.Latency.P99Ms:F3}` ms").ConfigureAwait(false);
        await writer.WriteLineAsync().ConfigureAwait(false);
        await writer.WriteLineAsync("## Failures").ConfigureAwait(false);
        if (summary.FailureCounts.Count == 0)
        {
            await writer.WriteLineAsync("- None observed.").ConfigureAwait(false);
        }
        else
        {
            foreach (var failure in summary.FailureCounts)
            {
                await writer.WriteLineAsync($"- `{failure.Key}`: `{failure.Value}`").ConfigureAwait(false);
            }
        }
    }
}
