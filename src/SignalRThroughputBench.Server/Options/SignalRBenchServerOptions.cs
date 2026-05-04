namespace SignalRThroughputBench.Server.Options;

public sealed class SignalRBenchServerOptions
{
    public string[] Protocols { get; init; } = ["json"];
    public string Backplane { get; init; } = "none";
    public string? RedisConnection { get; init; }
    public bool EnableDetailedErrors { get; init; }
    public long MaxReceiveMessageSizeBytes { get; init; } = 32_768;
}
