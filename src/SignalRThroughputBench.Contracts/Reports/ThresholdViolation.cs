namespace SignalRThroughputBench.Contracts.Reports;

public sealed record ThresholdViolation(
    string Metric,
    string Operator,
    double Expected,
    double Actual,
    string Message);
