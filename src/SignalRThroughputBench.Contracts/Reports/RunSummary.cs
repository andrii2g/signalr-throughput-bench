namespace SignalRThroughputBench.Contracts.Reports;

public sealed record RunSummary
{
    public int SchemaVersion { get; init; } = 1;
    public required string RunId { get; init; }
    public required DateTimeOffset StartedAtUtc { get; init; }
    public required DateTimeOffset EndedAtUtc { get; init; }
    public required string Scenario { get; init; }
    public required string ServerUrl { get; init; }
    public required string Protocol { get; init; }
    public required string TransportRequested { get; init; }
    public required string TransportObserved { get; init; }
    public required int Connections { get; init; }
    public required int PayloadBytes { get; init; }
    public required int WarmupSeconds { get; init; }
    public required int DurationSeconds { get; init; }
    public required long TotalOperations { get; init; }
    public required long FailedOperations { get; init; }
    public required double OperationsPerSecond { get; init; }
    public required LatencySummary Latency { get; init; }
    public required ResourceSummary Resources { get; init; }
    public required EnvironmentSummary Environment { get; init; }
    public required ThresholdSummary Thresholds { get; init; }
    public required string LatencySamplingMode { get; init; }
    public required IReadOnlyDictionary<string, int> FailureCounts { get; init; }
}
