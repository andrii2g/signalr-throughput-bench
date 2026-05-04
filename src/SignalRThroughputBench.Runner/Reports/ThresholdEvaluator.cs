using System.Text.Json;
using SignalRThroughputBench.Contracts.Reports;

namespace SignalRThroughputBench.Runner.Reports;

public static class ThresholdEvaluator
{
    public static ThresholdSummary Evaluate(string? path, RunSummary summary)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new ThresholdSummary(false, null, []);
        }

        var document = JsonDocument.Parse(File.ReadAllText(path));
        var violations = new List<ThresholdViolation>();
        foreach (var rule in document.RootElement.GetProperty("rules").EnumerateArray())
        {
            var metric = rule.GetProperty("metric").GetString() ?? string.Empty;
            var op = rule.GetProperty("operator").GetString() ?? "==";
            var expected = rule.GetProperty("value").GetDouble();
            var actual = ResolveMetric(metric, summary);
            if (!Compare(actual, expected, op))
            {
                violations.Add(new ThresholdViolation(metric, op, expected, actual, $"{metric} expected {op} {expected} but was {actual}."));
            }
        }

        return new ThresholdSummary(true, violations.Count == 0, violations);
    }

    private static double ResolveMetric(string metric, RunSummary summary) => metric switch
    {
        "operationsPerSecond" => summary.OperationsPerSecond,
        "failedOperations" => summary.FailedOperations,
        "latency.p95Ms" => summary.Latency.P95Ms,
        _ => 0d
    };

    private static bool Compare(double actual, double expected, string op) => op switch
    {
        "<" => actual < expected,
        "<=" => actual <= expected,
        ">" => actual > expected,
        ">=" => actual >= expected,
        "==" => Math.Abs(actual - expected) < double.Epsilon,
        "!=" => Math.Abs(actual - expected) >= double.Epsilon,
        _ => false
    };
}
