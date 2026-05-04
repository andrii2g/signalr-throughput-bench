namespace SignalRThroughputBench.Runner.Options;

public sealed record RunnerOptions
{
    public string ServerUrl { get; init; } = "http://localhost:5080/bench";
    public string Scenario { get; init; } = string.Empty;
    public int Connections { get; init; } = 100;
    public int DurationSeconds { get; init; } = 30;
    public int WarmupSeconds { get; init; } = 10;
    public int CooldownSeconds { get; init; } = 3;
    public int PayloadBytes { get; init; } = 256;
    public BenchProtocol Protocol { get; init; } = BenchProtocol.Json;
    public BenchTransport Transport { get; init; } = BenchTransport.WebSocket;
    public int? SendRate { get; init; }
    public int Groups { get; init; } = 10;
    public int Targets { get; init; } = 100;
    public int ParallelConnect { get; init; } = 50;
    public string OutputDirectory { get; init; } = Path.Combine("results", $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-run");
    public string? ConfigFile { get; init; }
    public string? RunId { get; init; }
    public string? ThresholdFile { get; init; }
    public bool FailOnThreshold { get; init; }
    public bool Verbose { get; init; }
}
