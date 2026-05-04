using SignalRThroughputBench.Runner.Metrics;

namespace SignalRThroughputBench.Runner.Reports;

public static class CsvReportWriter
{
    public static async Task WriteLatencyAsync(string path, IEnumerable<LatencyRecord> records, CancellationToken cancellationToken) =>
        await WriteAsync(path, "elapsed_ms,operation,latency_ms,success,error_type", records.Select(static record =>
            $"{record.ElapsedMilliseconds:F3},{Escape(record.Operation)},{record.LatencyMilliseconds:F3},{record.Success.ToString().ToLowerInvariant()},{Escape(record.ErrorType)}"), cancellationToken).ConfigureAwait(false);

    public static async Task WriteThroughputAsync(string path, IEnumerable<ThroughputRecord> records, CancellationToken cancellationToken) =>
        await WriteAsync(path, "elapsed_second,operations,failures,operations_per_second", records.Select(static record =>
            $"{record.ElapsedSecond},{record.Operations},{record.Failures},{record.OperationsPerSecond:F3}"), cancellationToken).ConfigureAwait(false);

    public static async Task WriteResourcesAsync(string path, IEnumerable<ResourceRecord> records, CancellationToken cancellationToken) =>
        await WriteAsync(path, "elapsed_second,process,working_set_mb,private_memory_mb,cpu_percent,thread_count,gc_heap_mb,gen0,gen1,gen2", records.Select(static record =>
            $"{record.ElapsedSecond},{record.Process},{Format(record.WorkingSetMb)},{Format(record.PrivateMemoryMb)},{Format(record.CpuPercent)},{record.ThreadCount},{Format(record.GcHeapMb)},{record.Gen0},{record.Gen1},{record.Gen2}"), cancellationToken).ConfigureAwait(false);

    public static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }

    private static string Format(double? value) => value.HasValue ? value.Value.ToString("F3") : string.Empty;

    private static async Task WriteAsync(string path, string header, IEnumerable<string> rows, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var writer = new StreamWriter(path);
        await writer.WriteLineAsync(header).ConfigureAwait(false);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(row).ConfigureAwait(false);
        }
    }
}
