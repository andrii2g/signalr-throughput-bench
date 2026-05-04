using SignalRThroughputBench.Runner.Cli;
using SignalRThroughputBench.Runner.Options;

namespace SignalRThroughputBench.Tests;

public sealed class OptionsParsingTests
{
    [Fact]
    public void ParsesProtocolAndConnections()
    {
        var options = RunCommand.Parse(["run", "--scenario", "echo", "--connections", "12", "--protocol", "messagepack"]);
        Assert.Equal("echo", options.Scenario);
        Assert.Equal(12, options.Connections);
        Assert.Equal(BenchProtocol.MessagePack, options.Protocol);
    }
}
