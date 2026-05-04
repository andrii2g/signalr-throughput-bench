namespace SignalRThroughputBench.Runner.Options;

public enum BenchProtocol
{
    Json,
    MessagePack
}

public enum BenchTransport
{
    Auto,
    WebSocket,
    LongPolling,
    ServerSentEvents
}
