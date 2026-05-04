using SignalRThroughputBench.Runner.Metrics;

namespace SignalRThroughputBench.Tests;

public sealed class LatencyHistogramTests
{
    [Fact]
    public void EmptyHistogramReturnsZeroSummary()
    {
        var histogram = new LatencyHistogram();
        var summary = histogram.ToSummary();
        Assert.Equal(0d, summary.P50Ms);
        Assert.Equal(0d, summary.P99Ms);
    }

    [Fact]
    public void PercentilesAreCalculated()
    {
        var histogram = new LatencyHistogram();
        histogram.Record(TimeSpan.FromMilliseconds(1));
        histogram.Record(TimeSpan.FromMilliseconds(2));
        histogram.Record(TimeSpan.FromMilliseconds(3));
        var summary = histogram.ToSummary();
        Assert.Equal(2d, summary.P50Ms);
        Assert.Equal(3d, summary.MaxMs);
    }
}
