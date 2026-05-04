namespace SignalRThroughputBench.Contracts.Reports;

public sealed record ResourceSummary(
    double? RunnerMaxWorkingSetMb,
    double? ServerMaxWorkingSetMb,
    double? RunnerCpuPercentAvg,
    double? ServerCpuPercentAvg);
