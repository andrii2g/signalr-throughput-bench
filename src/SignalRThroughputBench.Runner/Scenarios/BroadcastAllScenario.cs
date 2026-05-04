using System.Diagnostics;
using SignalRThroughputBench.Contracts.Payloads;
using SignalRThroughputBench.Runner.Load;
using SignalRThroughputBench.Runner.Metrics;
using SignalRThroughputBench.Runner.Options;

namespace SignalRThroughputBench.Runner.Scenarios;

public sealed class BroadcastAllScenario : IBenchScenario
{
    public string Name => "broadcast-all";

    public async Task<ScenarioExecutionResult> ExecuteAsync(RunnerOptions options, CancellationToken cancellationToken)
    {
        var metrics = new RunMetrics();
        var clients = await ScenarioHelpers.ConnectClientsAsync(options, deterministicUsers: false, cancellationToken).ConfigureAwait(false);
        var coordinator = clients[0];
        var recipients = clients.Skip(1).ToArray();
        var sampler = new ResourceSampler(new HttpClient(), options.ServerUrl.Replace("/bench", "/metrics/snapshot", StringComparison.OrdinalIgnoreCase));
        var limiter = new RateLimiter(options.SendRate);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            using var measurementCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            measurementCts.CancelAfter(TimeSpan.FromSeconds(options.DurationSeconds));
            var resourcesTask = sampler.CaptureAsync(TimeSpan.FromSeconds(options.DurationSeconds), cancellationToken);
            var sequence = 0;

            while (!measurementCts.IsCancellationRequested)
            {
                await limiter.WaitAsync(measurementCts.Token).ConfigureAwait(false);
                var payload = ScenarioHelpers.CreatePayload(sequence++, options.PayloadBytes);
                var started = stopwatch.Elapsed;
                try
                {
                    await coordinator.BroadcastAsync(new BroadcastRequest(payload), measurementCts.Token).ConfigureAwait(false);
                    await ScenarioHelpers.WaitForDeliveriesAsync(recipients, Name, payload.MessageId, stopwatch, started, metrics, measurementCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (measurementCts.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                    metrics.RecordFailure(Name, started, "send_failed");
                }
            }

            return new ScenarioExecutionResult(metrics, await resourcesTask.ConfigureAwait(false), options.Transport.ToString().ToLowerInvariant());
        }
        finally
        {
            await ScenarioHelpers.DisposeClientsAsync(clients).ConfigureAwait(false);
        }
    }
}
