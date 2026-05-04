using SignalRThroughputBench.Runner.Metrics;
using SignalRThroughputBench.Runner.Options;

namespace SignalRThroughputBench.Runner.Scenarios;

public sealed class IdleConnectionsScenario : IBenchScenario
{
    public string Name => "idle-connections";

    public async Task<ScenarioExecutionResult> ExecuteAsync(RunnerOptions options, CancellationToken cancellationToken)
    {
        var metrics = new RunMetrics();
        var clients = await ScenarioHelpers.ConnectClientsAsync(options, deterministicUsers: false, cancellationToken).ConfigureAwait(false);
        var sampler = new ResourceSampler(new HttpClient(), options.ServerUrl.Replace("/bench", "/metrics/snapshot", StringComparison.OrdinalIgnoreCase));

        try
        {
            var resources = await sampler.CaptureAsync(TimeSpan.FromSeconds(options.DurationSeconds), cancellationToken).ConfigureAwait(false);
            return new ScenarioExecutionResult(metrics, resources, options.Transport.ToString().ToLowerInvariant());
        }
        finally
        {
            await ScenarioHelpers.DisposeClientsAsync(clients).ConfigureAwait(false);
        }
    }
}
