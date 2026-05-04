using SignalRThroughputBench.Runner.Load;

namespace SignalRThroughputBench.Tests;

public sealed class PayloadGenerationTests
{
    [Fact]
    public void PayloadGenerationKeepsRequestedSize()
    {
        var payload = PayloadFactory.Create(1, 256);
        Assert.Equal(256, payload.PayloadBytes);
        Assert.NotEmpty(payload.Data);
    }
}
