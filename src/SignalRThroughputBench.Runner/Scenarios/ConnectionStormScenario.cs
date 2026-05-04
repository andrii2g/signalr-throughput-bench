using System.Diagnostics;
using SignalRThroughputBench.Runner.Clients;
using SignalRThroughputBench.Runner.Metrics;
using SignalRThroughputBench.Runner.Options;

namespace SignalRThroughputBench.Runner.Scenarios;

public sealed class ConnectionStormScenario : IBenchScenario
{
    public string Name => "connection-storm";

    public async Task<ScenarioExecutionResult> ExecuteAsync(RunnerOptions options, CancellationToken cancellationToken)
    {
        var metrics = new RunMetrics();
        var stopwatch = Stopwatch.StartNew();
        var clients = new List<SignalRBenchClient>();
        var semaphore = new SemaphoreSlim(options.ParallelConnect);
        var tasks = Enumerable.Range(0, options.Connections).Select(async _ =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            var started = stopwatch.Elapsed;
            try
            {
                var client = SignalRClientFactory.Create(options);
                await client.StartAsync(cancellationToken).ConfigureAwait(false);
                lock (clients)
                {
                    clients.Add(client);
                }
                metrics.RecordSuccess(Name, started, stopwatch.Elapsed - started);
            }
            catch
            {
                metrics.RecordFailure(Name, started, "connection_failed");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        await ScenarioHelpers.DisposeClientsAsync(clients).ConfigureAwait(false);
        return new ScenarioExecutionResult(metrics, [], options.Transport.ToString().ToLowerInvariant());
    }
}
