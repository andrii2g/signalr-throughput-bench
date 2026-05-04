namespace SignalRThroughputBench.Runner.Load;

public sealed class RateLimiter
{
    private readonly TimeSpan _interval;
    private DateTimeOffset _next = DateTimeOffset.UtcNow;

    public RateLimiter(int? operationsPerSecond)
    {
        _interval = operationsPerSecond is > 0 ? TimeSpan.FromSeconds(1d / operationsPerSecond.Value) : TimeSpan.Zero;
    }

    public async Task WaitAsync(CancellationToken cancellationToken)
    {
        if (_interval == TimeSpan.Zero)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (_next > now)
        {
            await Task.Delay(_next - now, cancellationToken).ConfigureAwait(false);
        }

        _next = DateTimeOffset.UtcNow + _interval;
    }
}
