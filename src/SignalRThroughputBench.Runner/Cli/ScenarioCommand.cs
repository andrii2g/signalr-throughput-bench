namespace SignalRThroughputBench.Runner.Cli;

public static class ScenarioCommand
{
    public static IReadOnlyList<string> Names =>
    [
        "echo",
        "broadcast-all",
        "group-broadcast",
        "targeted-user",
        "connection-storm",
        "idle-connections"
    ];
}
