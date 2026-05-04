using System.Diagnostics;
using SignalRThroughputBench.Contracts.Payloads;
using SignalRThroughputBench.Runner.Metrics;
using SignalRThroughputBench.Runner.Options;

namespace SignalRThroughputBench.Runner.Scenarios;

public sealed class EchoScenario : IBenchScenario
{
    public string Name => "echo";

    public async Task<ScenarioExecutionResult> ExecuteAsync(RunnerOptions options, CancellationToken cancellationToken)
    {
        var metrics = new RunMetrics();
        var clients = await ScenarioHelpers.ConnectClientsAsync(options, deterministicUsers: false, cancellationToken).ConfigureAwait(false);
        var sampler = new ResourceSampler(new HttpClient(), options.ServerUrl.Replace("/bench", "/metrics/snapshot", StringComparison.OrdinalIgnoreCase));

        try
        {
            await ScenarioHelpers.WarmupAsync(async ct =>
            {
                var payload = ScenarioHelpers.CreatePayload(0, options.PayloadBytes);
                await clients[0].EchoAsync(new EchoRequest(payload), ct).ConfigureAwait(false);
            }, options, cancellationToken).ConfigureAwait(false);

            var stopwatch = Stopwatch.StartNew();
            using var measurementCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            measurementCts.CancelAfter(TimeSpan.FromSeconds(options.DurationSeconds));

            var tasks = clients.Select(async (client, clientIndex) =>
            {
                var sequence = clientIndex;
                while (!measurementCts.IsCancellationRequested)
                {
                    var started = stopwatch.Elapsed;
                    try
                    {
                        var payload = ScenarioHelpers.CreatePayload(sequence++, options.PayloadBytes);
                        await client.EchoAsync(new EchoRequest(payload), measurementCts.Token).ConfigureAwait(false);
                        metrics.RecordSuccess(Name, started, stopwatch.Elapsed - started);
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
            });

            var resourcesTask = sampler.CaptureAsync(TimeSpan.FromSeconds(options.DurationSeconds), cancellationToken);
            await Task.WhenAll(tasks).ConfigureAwait(false);
            return new ScenarioExecutionResult(metrics, await resourcesTask.ConfigureAwait(false), options.Transport.ToString().ToLowerInvariant());
        }
        finally
        {
            await ScenarioHelpers.DisposeClientsAsync(clients).ConfigureAwait(false);
        }
    }
}
