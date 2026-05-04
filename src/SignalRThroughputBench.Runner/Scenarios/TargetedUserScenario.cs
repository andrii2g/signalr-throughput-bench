using System.Diagnostics;
using SignalRThroughputBench.Contracts.Payloads;
using SignalRThroughputBench.Runner.Load;
using SignalRThroughputBench.Runner.Metrics;
using SignalRThroughputBench.Runner.Options;

namespace SignalRThroughputBench.Runner.Scenarios;

public sealed class TargetedUserScenario : IBenchScenario
{
    public string Name => "targeted-user";

    public async Task<ScenarioExecutionResult> ExecuteAsync(RunnerOptions options, CancellationToken cancellationToken)
    {
        var metrics = new RunMetrics();
        var clients = await ScenarioHelpers.ConnectClientsAsync(options, deterministicUsers: true, cancellationToken).ConfigureAwait(false);
        var coordinator = clients[0];
        var recipients = clients.Skip(1).ToArray();
        var sampler = new ResourceSampler(new HttpClient(), options.ServerUrl.Replace("/bench", "/metrics/snapshot", StringComparison.OrdinalIgnoreCase));
        var limiter = new RateLimiter(options.SendRate);
        var stopwatch = Stopwatch.StartNew();
        using var measurementCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        measurementCts.CancelAfter(TimeSpan.FromSeconds(options.DurationSeconds));
        var resourcesTask = sampler.CaptureAsync(TimeSpan.FromSeconds(options.DurationSeconds), cancellationToken);
        var sequence = 0;

        try
        {
            while (!measurementCts.IsCancellationRequested)
            {
                await limiter.WaitAsync(measurementCts.Token).ConfigureAwait(false);
                var targetIndex = sequence % Math.Max(1, Math.Min(recipients.Length, options.Targets));
                var userId = $"user-{targetIndex + 1:D4}";
                var payload = ScenarioHelpers.CreatePayload(sequence++, options.PayloadBytes);
                var started = stopwatch.Elapsed;
                try
                {
                    await coordinator.SendToUserAsync(new TargetedUserRequest(userId, payload), measurementCts.Token).ConfigureAwait(false);
                    await ScenarioHelpers.WaitForDeliveriesAsync([recipients[targetIndex]], Name, payload.MessageId, stopwatch, started, metrics, measurementCts.Token).ConfigureAwait(false);
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
