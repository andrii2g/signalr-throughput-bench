namespace SignalRThroughputBench.Contracts.Payloads;

public sealed record EchoResponse(
    string MessageId,
    int Sequence,
    int PayloadBytes,
    long ServerTimestampUtcTicks);
