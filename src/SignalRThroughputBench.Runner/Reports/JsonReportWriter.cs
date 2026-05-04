using System.Text.Json;
using SignalRThroughputBench.Contracts.Reports;

namespace SignalRThroughputBench.Runner.Reports;

public static class JsonReportWriter
{
    public static async Task WriteAsync(string path, RunSummary summary, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, summary, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        }, cancellationToken).ConfigureAwait(false);
    }
}
