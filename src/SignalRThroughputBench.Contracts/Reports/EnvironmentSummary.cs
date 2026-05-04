namespace SignalRThroughputBench.Contracts.Reports;

public sealed record EnvironmentSummary(
    string DotnetVersion,
    string Os,
    bool Containerized,
    int ProcessorCount);
