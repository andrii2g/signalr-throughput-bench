namespace SignalRThroughputBench.Contracts.Payloads;

public sealed record BenchPayload(
    string MessageId,
    int Sequence,
    long RunnerSendStartStopwatchTicks,
    int PayloadBytes,
    string Data);
