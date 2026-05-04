namespace SignalRThroughputBench.Contracts.Payloads;

public sealed record GroupBroadcastRequest(
    string GroupName,
    BenchPayload Payload);
