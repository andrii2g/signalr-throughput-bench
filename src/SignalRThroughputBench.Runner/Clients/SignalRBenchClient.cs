using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR.Client;
using SignalRThroughputBench.Contracts.Payloads;

namespace SignalRThroughputBench.Runner.Clients;

public sealed class SignalRBenchClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ConcurrentQueue<BenchPayload> _received = new();

    public SignalRBenchClient(HubConnection connection)
    {
        _connection = connection;
        _connection.On<BenchPayload>("Receive", payload => _received.Enqueue(payload));
    }

    public string ConnectionId => _connection.ConnectionId ?? string.Empty;
    public bool TryDequeueReceived(out BenchPayload? payload) => _received.TryDequeue(out payload);
    public Task StartAsync(CancellationToken cancellationToken) => _connection.StartAsync(cancellationToken);
    public Task<EchoResponse> EchoAsync(EchoRequest request, CancellationToken cancellationToken) => _connection.InvokeAsync<EchoResponse>("Echo", request, cancellationToken);
    public Task BroadcastAsync(BroadcastRequest request, CancellationToken cancellationToken) => _connection.InvokeAsync("Broadcast", request, cancellationToken);
    public Task BroadcastGroupAsync(GroupBroadcastRequest request, CancellationToken cancellationToken) => _connection.InvokeAsync("BroadcastGroup", request, cancellationToken);
    public Task JoinGroupAsync(string groupName, CancellationToken cancellationToken) => _connection.InvokeAsync("JoinGroup", groupName, cancellationToken);
    public Task SendToUserAsync(TargetedUserRequest request, CancellationToken cancellationToken) => _connection.InvokeAsync("SendToUser", request, cancellationToken);
    public ValueTask DisposeAsync() => _connection.DisposeAsync();
}
