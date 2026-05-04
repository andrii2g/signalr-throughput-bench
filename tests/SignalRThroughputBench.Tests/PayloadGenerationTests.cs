using SignalRThroughputBench.Runner.Scenarios;

namespace SignalRThroughputBench.Tests;

public sealed class PayloadGenerationTests
{
    [Fact]
    public void PayloadGenerationKeepsRequestedSize()
    {
        var payload = ScenarioHelpers.CreatePayload(1, 256);
        Assert.Equal(256, payload.PayloadBytes);
        Assert.NotEmpty(payload.Data);
    }
}
