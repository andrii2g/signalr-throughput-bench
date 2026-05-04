using System.Diagnostics;
using SignalRThroughputBench.Contracts.Payloads;
using SignalRThroughputBench.Runner.Clients;
using SignalRThroughputBench.Runner.Load;
using SignalRThroughputBench.Runner.Metrics;
using SignalRThroughputBench.Runner.Options;

namespace SignalRThroughputBench.Runner.Scenarios;

public static class ScenarioHelpers
{
    public static async Task<List<SignalRBenchClient>> ConnectClientsAsync(RunnerOptions options, bool deterministicUsers, CancellationToken cancellationToken)
    {
        var clients = new List<SignalRBenchClient>(options.Connections);
        var semaphore = new SemaphoreSlim(options.ParallelConnect);
        var tasks = Enumerable.Range(0, options.Connections).Select(async index =>
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var client = SignalRClientFactory.Create(options, deterministicUsers ? $"user-{index:D4}" : null);
                await client.StartAsync(cancellationToken).ConfigureAwait(false);
                lock (clients)
                {
                    clients.Add(client);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return clients;
    }

    public static BenchPayload CreatePayload(int sequence, int payloadBytes)
    {
        var dataLength = Math.Max(1, payloadBytes - 64);
        return new BenchPayload(
            Guid.NewGuid().ToString("N"),
            sequence,
            Stopwatch.GetTimestamp(),
            payloadBytes,
            new string('A', dataLength));
    }

    public static async Task DisposeClientsAsync(IEnumerable<SignalRBenchClient> clients)
    {
        foreach (var client in clients)
        {
            try
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }

    public static async Task WaitForDeliveriesAsync(IEnumerable<SignalRBenchClient> recipients, string operation, string messageId, Stopwatch stopwatch, TimeSpan started, RunMetrics metrics, CancellationToken cancellationToken)
    {
        var pending = recipients.ToList();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (pending.Count > 0 && DateTime.UtcNow < deadline)
        {
            for (var index = pending.Count - 1; index >= 0; index--)
            {
                if (pending[index].TryDequeueReceived(out var payload) && payload?.MessageId == messageId)
                {
                    metrics.RecordSuccess(operation, started, stopwatch.Elapsed - started);
                    pending.RemoveAt(index);
                }
            }

            if (pending.Count > 0)
            {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (var _ in pending)
        {
            metrics.RecordFailure(operation, started, "receive_timeout");
        }
    }

    public static Task WarmupAsync(Func<CancellationToken, Task> action, RunnerOptions options, CancellationToken cancellationToken) =>
        WarmupController.RunAsync(action, options.WarmupSeconds, cancellationToken);
}
