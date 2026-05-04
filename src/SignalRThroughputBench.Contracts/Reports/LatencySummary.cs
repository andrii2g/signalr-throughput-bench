namespace SignalRThroughputBench.Contracts.Reports;

public sealed record LatencySummary(
    double P50Ms,
    double P75Ms,
    double P90Ms,
    double P95Ms,
    double P99Ms,
    double MaxMs);
