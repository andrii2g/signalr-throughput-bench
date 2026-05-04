using System.Diagnostics;

namespace SignalRThroughputBench.Server.Metrics;

public sealed class ServerMetrics
{
    private long _connectedClients;
    private long _echoCalls;
    private long _broadcastMessages;
    private long _groupMessages;
    private long _targetedMessages;
    private readonly DateTimeOffset _startedAtUtc = DateTimeOffset.UtcNow;

    public void ConnectionOpened() => Interlocked.Increment(ref _connectedClients);
    public void ConnectionClosed() => Interlocked.Decrement(ref _connectedClients);
    public void EchoCalled() => Interlocked.Increment(ref _echoCalls);
    public void BroadcastSent() => Interlocked.Increment(ref _broadcastMessages);
    public void GroupSent() => Interlocked.Increment(ref _groupMessages);
    public void TargetedSent() => Interlocked.Increment(ref _targetedMessages);

    public object CreateSnapshot()
    {
        var process = Process.GetCurrentProcess();
        return new
        {
            startedAtUtc = _startedAtUtc,
            connectedClients = Interlocked.Read(ref _connectedClients),
            echoCalls = Interlocked.Read(ref _echoCalls),
            broadcastMessages = Interlocked.Read(ref _broadcastMessages),
            groupMessages = Interlocked.Read(ref _groupMessages),
            targetedMessages = Interlocked.Read(ref _targetedMessages),
            workingSetMb = process.WorkingSet64 / 1024d / 1024d,
            privateMemoryMb = process.PrivateMemorySize64 / 1024d / 1024d,
            threadCount = process.Threads.Count,
            gcHeapMb = GC.GetTotalMemory(false) / 1024d / 1024d,
            gen0 = GC.CollectionCount(0),
            gen1 = GC.CollectionCount(1),
            gen2 = GC.CollectionCount(2)
        };
    }
}
