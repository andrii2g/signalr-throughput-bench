using SignalRThroughputBench.Runner.Options;

namespace SignalRThroughputBench.Runner.Scenarios;

public interface IBenchScenario
{
    string Name { get; }
    Task<ScenarioExecutionResult> ExecuteAsync(RunnerOptions options, CancellationToken cancellationToken);
}
