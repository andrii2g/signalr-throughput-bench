using SignalRThroughputBench.Contracts.Reports;

namespace SignalRThroughputBench.Runner.Metrics;

public sealed class LatencyHistogram
{
    private readonly List<double> _samples = [];
    private readonly int _cap;
    private bool _capReached;

    public LatencyHistogram(int cap = 5_000_000)
    {
        _cap = cap;
    }

    public string SamplingMode => _capReached ? "capped" : "full";

    public void Record(TimeSpan latency)
    {
        if (_samples.Count >= _cap)
        {
            _capReached = true;
            return;
        }

        _samples.Add(Math.Max(0d, latency.TotalMilliseconds));
    }

    public LatencySummary ToSummary()
    {
        if (_samples.Count == 0)
        {
            return new LatencySummary(0, 0, 0, 0, 0, 0);
        }

        var ordered = _samples.OrderBy(static value => value).ToArray();
        return new LatencySummary(
            Percentile(ordered, 0.50),
            Percentile(ordered, 0.75),
            Percentile(ordered, 0.90),
            Percentile(ordered, 0.95),
            Percentile(ordered, 0.99),
            ordered[^1]);
    }

    private static double Percentile(IReadOnlyList<double> ordered, double percentile)
    {
        var position = (ordered.Count - 1) * percentile;
        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);
        if (lowerIndex == upperIndex)
        {
            return ordered[lowerIndex];
        }

        var weight = position - lowerIndex;
        return ordered[lowerIndex] + ((ordered[upperIndex] - ordered[lowerIndex]) * weight);
    }
}
