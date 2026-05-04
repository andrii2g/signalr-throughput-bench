using Microsoft.AspNetCore.SignalR;

namespace SignalRThroughputBench.Server.Identity;

public sealed class QueryStringUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var userId = connection.GetHttpContext()?.Request.Query["userId"].ToString();
        return string.IsNullOrWhiteSpace(userId) ? connection.ConnectionId : userId;
    }
}
