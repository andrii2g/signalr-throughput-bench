namespace SignalRThroughputBench.Runner.Load;

public static class ClientGroupPlanner
{
    public static IReadOnlyList<string> BuildGroups(int connectionCount, int groupCount)
    {
        if (connectionCount <= 0)
        {
            return [];
        }

        groupCount = Math.Max(1, groupCount);
        var groups = new List<string>(connectionCount);
        for (var index = 0; index < connectionCount; index++)
        {
            groups.Add($"group-{index % groupCount:D3}");
        }

        return groups;
    }
}
