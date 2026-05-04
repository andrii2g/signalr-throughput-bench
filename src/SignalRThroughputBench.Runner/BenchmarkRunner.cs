using SignalRThroughputBench.Contracts.Reports;
using SignalRThroughputBench.Runner.Options;
using SignalRThroughputBench.Runner.Reports;
using SignalRThroughputBench.Runner.Scenarios;

namespace SignalRThroughputBench.Runner;

public sealed class BenchmarkRunner
{
    private readonly IReadOnlyDictionary<string, IBenchScenario> _scenarios = new Dictionary<string, IBenchScenario>(StringComparer.OrdinalIgnoreCase)
    {
        ["echo"] = new EchoScenario(),
        ["broadcast-all"] = new BroadcastAllScenario(),
        ["group-broadcast"] = new GroupBroadcastScenario(),
        ["targeted-user"] = new TargetedUserScenario(),
        ["connection-storm"] = new ConnectionStormScenario(),
        ["idle-connections"] = new IdleConnectionsScenario()
    };

    public async Task<int> RunAsync(RunnerOptions options, CancellationToken cancellationToken)
    {
        if (!_scenarios.TryGetValue(options.Scenario, out var scenario))
        {
            Console.Error.WriteLine($"Unknown scenario '{options.Scenario}'.");
            return 1;
        }

        Directory.CreateDirectory(options.OutputDirectory);
        var startedAt = DateTimeOffset.UtcNow;
        var execution = await scenario.ExecuteAsync(options, cancellationToken).ConfigureAwait(false);
        var endedAt = DateTimeOffset.UtcNow;
        var summary = CreateSummary(options, execution, startedAt, endedAt);
        var thresholdSummary = ThresholdEvaluator.Evaluate(options.ThresholdFile, summary);
        summary = summary with { Thresholds = thresholdSummary };

        await JsonReportWriter.WriteAsync(Path.Combine(options.OutputDirectory, "run-summary.json"), summary, cancellationToken).ConfigureAwait(false);
        await CsvReportWriter.WriteLatencyAsync(Path.Combine(options.OutputDirectory, "latency.csv"), execution.Metrics.Latencies, cancellationToken).ConfigureAwait(false);
        await CsvReportWriter.WriteThroughputAsync(Path.Combine(options.OutputDirectory, "throughput.csv"), execution.Metrics.Throughput, cancellationToken).ConfigureAwait(false);
        await CsvReportWriter.WriteResourcesAsync(Path.Combine(options.OutputDirectory, "resources.csv"), execution.Resources, cancellationToken).ConfigureAwait(false);
        await MarkdownReportWriter.WriteAsync(Path.Combine(options.OutputDirectory, "report.md"), summary, cancellationToken).ConfigureAwait(false);

        return options.FailOnThreshold && thresholdSummary.Passed is false ? 2 : 0;
    }

    private static RunSummary CreateSummary(RunnerOptions options, ScenarioExecutionResult execution, DateTimeOffset startedAt, DateTimeOffset endedAt)
    {
        var totalOperations = execution.Metrics.SuccessCount + execution.Metrics.FailureCount;
        var runnerResources = execution.Resources.Where(static record => record.Process == "runner").ToArray();
        var serverResources = execution.Resources.Where(static record => record.Process == "server").ToArray();
        return new RunSummary
        {
            RunId = options.RunId ?? $"{startedAt:yyyy-MM-ddTHH-mm-ssZ}-{options.Scenario}-{options.Protocol.ToString().ToLowerInvariant()}-{options.Transport.ToString().ToLowerInvariant()}",
            StartedAtUtc = startedAt,
            EndedAtUtc = endedAt,
            Scenario = options.Scenario,
            ServerUrl = options.ServerUrl,
            Protocol = options.Protocol == BenchProtocol.MessagePack ? "messagepack" : "json",
            TransportRequested = options.Transport.ToString().ToLowerInvariant(),
            TransportObserved = execution.TransportObserved,
            Connections = options.Connections,
            PayloadBytes = options.PayloadBytes,
            WarmupSeconds = options.WarmupSeconds,
            DurationSeconds = options.DurationSeconds,
            TotalOperations = totalOperations,
            FailedOperations = execution.Metrics.FailureCount,
            OperationsPerSecond = options.DurationSeconds > 0 ? execution.Metrics.SuccessCount / (double)options.DurationSeconds : execution.Metrics.SuccessCount,
            Latency = execution.Metrics.CreateLatencySummary(),
            Resources = new ResourceSummary(
                runnerResources.MaxBy(static record => record.WorkingSetMb)?.WorkingSetMb,
                serverResources.MaxBy(static record => record.WorkingSetMb)?.WorkingSetMb,
                null,
                null),
            Environment = new EnvironmentSummary(
                Environment.Version.ToString(),
                Environment.OSVersion.ToString(),
                string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase),
                Environment.ProcessorCount),
            Thresholds = new ThresholdSummary(false, null, []),
            LatencySamplingMode = execution.Metrics.LatencySamplingMode,
            FailureCounts = new Dictionary<string, int>(execution.Metrics.Failures, StringComparer.OrdinalIgnoreCase)
        };
    }
}
