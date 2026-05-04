namespace SignalRThroughputBench.Runner.Options;

public sealed record ScenarioOptions(
    int Connections,
    int DurationSeconds,
    int WarmupSeconds,
    int CooldownSeconds,
    int PayloadBytes,
    int Groups,
    int Targets,
    int? SendRate);
