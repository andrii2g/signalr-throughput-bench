namespace SignalRThroughputBench.Contracts.Reports;

public sealed record ThresholdSummary(
    bool Evaluated,
    bool? Passed,
    IReadOnlyList<ThresholdViolation> Violations);
