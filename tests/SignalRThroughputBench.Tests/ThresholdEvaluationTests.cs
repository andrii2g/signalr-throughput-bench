using SignalRThroughputBench.Contracts.Reports;
using SignalRThroughputBench.Runner.Reports;

namespace SignalRThroughputBench.Tests;

public sealed class ThresholdEvaluationTests
{
    [Fact]
    public void ThresholdsPassWhenMetricMatches()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, """
            {
              "schemaVersion": 1,
              "rules": [
                { "metric": "operationsPerSecond", "operator": ">=", "value": 10 }
              ]
            }
            """);

            var summary = new RunSummary
            {
                RunId = "test",
                StartedAtUtc = DateTimeOffset.UtcNow,
                EndedAtUtc = DateTimeOffset.UtcNow,
                Scenario = "echo",
                ServerUrl = "http://localhost/bench",
                Protocol = "json",
                TransportRequested = "websocket",
                TransportObserved = "websocket",
                Connections = 1,
                PayloadBytes = 1,
                WarmupSeconds = 0,
                DurationSeconds = 1,
                TotalOperations = 1,
                FailedOperations = 0,
                OperationsPerSecond = 11,
                Latency = new LatencySummary(1, 1, 1, 1, 1, 1),
                Resources = new ResourceSummary(null, null, null, null),
                Environment = new EnvironmentSummary("10.0", "Windows", false, 1),
                Thresholds = new ThresholdSummary(false, null, []),
                LatencySamplingMode = "full",
                FailureCounts = new Dictionary<string, int>()
            };

            var result = ThresholdEvaluator.Evaluate(file, summary);
            Assert.True(result.Passed);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
