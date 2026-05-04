using System.Collections.Concurrent;
using SignalRThroughputBench.Contracts.Reports;

namespace SignalRThroughputBench.Runner.Metrics;

public sealed class RunMetrics
{
    private readonly ConcurrentDictionary<int, Bucket> _buckets = new();
    private readonly ConcurrentDictionary<string, int> _failures = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<LatencyRecord> _latencies = new();
    private readonly LatencyHistogram _histogram = new();
    private long _successCount;
    private long _failureCount;

    public void RecordSuccess(string operation, TimeSpan elapsed, TimeSpan latency)
    {
        _histogram.Record(latency);
        _latencies.Enqueue(new LatencyRecord(elapsed.TotalMilliseconds, operation, latency.TotalMilliseconds, true, string.Empty));
        var bucket = _buckets.GetOrAdd((int)Math.Floor(elapsed.TotalSeconds), static _ => new Bucket());
        Interlocked.Increment(ref bucket.Successes);
        Interlocked.Increment(ref _successCount);
    }

    public void RecordFailure(string operation, TimeSpan elapsed, string errorType)
    {
        _latencies.Enqueue(new LatencyRecord(elapsed.TotalMilliseconds, operation, 0d, false, errorType));
        var bucket = _buckets.GetOrAdd((int)Math.Floor(elapsed.TotalSeconds), static _ => new Bucket());
        Interlocked.Increment(ref bucket.Failures);
        _failures.AddOrUpdate(errorType, 1, static (_, current) => current + 1);
        Interlocked.Increment(ref _failureCount);
    }

    public LatencySummary CreateLatencySummary() => _histogram.ToSummary();
    public string LatencySamplingMode => _histogram.SamplingMode;
    public long SuccessCount => Interlocked.Read(ref _successCount);
    public long FailureCount => Interlocked.Read(ref _failureCount);
    public IReadOnlyDictionary<string, int> Failures => _failures;
    public IReadOnlyList<LatencyRecord> Latencies => _latencies.ToArray();
    public IReadOnlyList<ThroughputRecord> Throughput => _buckets.OrderBy(static pair => pair.Key)
        .Select(static pair => new ThroughputRecord(pair.Key, pair.Value.Successes, pair.Value.Failures))
        .ToArray();

    private sealed class Bucket
    {
        public int Successes;
        public int Failures;
    }
}

public sealed record LatencyRecord(double ElapsedMilliseconds, string Operation, double LatencyMilliseconds, bool Success, string ErrorType);
public sealed record ThroughputRecord(int ElapsedSecond, int Operations, int Failures)
{
    public double OperationsPerSecond => Operations;
}
