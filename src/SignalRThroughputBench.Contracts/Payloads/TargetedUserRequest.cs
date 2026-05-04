namespace SignalRThroughputBench.Contracts.Payloads;

public sealed record TargetedUserRequest(
    string UserId,
    BenchPayload Payload);
