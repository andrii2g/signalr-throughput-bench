using SignalRThroughputBench.Runner.Load;

namespace SignalRThroughputBench.Tests;

public sealed class ClientGroupPlannerTests
{
    [Fact]
    public void GroupsAreAssignedRoundRobin()
    {
        var groups = ClientGroupPlanner.BuildGroups(5, 2);
        Assert.Equal(["group-000", "group-001", "group-000", "group-001", "group-000"], groups);
    }
}
