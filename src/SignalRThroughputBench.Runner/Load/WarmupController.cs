namespace SignalRThroughputBench.Runner.Load;

public static class WarmupController
{
    public static async Task RunAsync(Func<CancellationToken, Task> action, int seconds, CancellationToken cancellationToken)
    {
        if (seconds <= 0)
        {
            return;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(seconds));
        try
        {
            await action(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
        }
    }
}
