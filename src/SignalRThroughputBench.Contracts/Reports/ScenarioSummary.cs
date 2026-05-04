namespace SignalRThroughputBench.Contracts.Reports;

public sealed record ScenarioSummary(
    string Name,
    IReadOnlyDictionary<string, int> Failures);
