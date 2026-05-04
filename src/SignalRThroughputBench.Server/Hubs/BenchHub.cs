using Microsoft.AspNetCore.SignalR;
using SignalRThroughputBench.Contracts.Payloads;
using SignalRThroughputBench.Server.Metrics;

namespace SignalRThroughputBench.Server.Hubs;

public sealed class BenchHub(ServerMetrics metrics) : Hub
{
    public override Task OnConnectedAsync()
    {
        metrics.ConnectionOpened();
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        metrics.ConnectionClosed();
        return base.OnDisconnectedAsync(exception);
    }

    public Task<EchoResponse> Echo(EchoRequest request)
    {
        metrics.EchoCalled();
        return Task.FromResult(new EchoResponse(
            request.Payload.MessageId,
            request.Payload.Sequence,
            request.Payload.PayloadBytes,
            DateTimeOffset.UtcNow.UtcTicks));
    }

    public Task JoinGroup(string groupName) => Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    public Task LeaveGroup(string groupName) => Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

    public async Task Broadcast(BroadcastRequest request)
    {
        metrics.BroadcastSent();
        await Clients.All.SendAsync("Receive", request.Payload).ConfigureAwait(false);
    }

    public async Task BroadcastGroup(GroupBroadcastRequest request)
    {
        metrics.GroupSent();
        await Clients.Group(request.GroupName).SendAsync("Receive", request.Payload).ConfigureAwait(false);
    }

    public async Task SendToUser(TargetedUserRequest request)
    {
        metrics.TargetedSent();
        await Clients.User(request.UserId).SendAsync("Receive", request.Payload).ConfigureAwait(false);
    }

    public Task Ping() => Task.CompletedTask;
}
