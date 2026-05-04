using System.Diagnostics;
using SignalRThroughputBench.Contracts.Payloads;

namespace SignalRThroughputBench.Runner.Load;

public static class PayloadFactory
{
    public static BenchPayload Create(int sequence, int payloadBytes)
    {
        var dataLength = Math.Max(1, payloadBytes - 64);
        return new BenchPayload(
            Guid.NewGuid().ToString("N"),
            sequence,
            Stopwatch.GetTimestamp(),
            payloadBytes,
            new string('A', dataLength));
    }
}
