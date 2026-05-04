using SignalRThroughputBench.Runner.Metrics;

namespace SignalRThroughputBench.Runner.Scenarios;

public sealed record ScenarioExecutionResult(
    RunMetrics Metrics,
    IReadOnlyList<ResourceRecord> Resources,
    string TransportObserved);
