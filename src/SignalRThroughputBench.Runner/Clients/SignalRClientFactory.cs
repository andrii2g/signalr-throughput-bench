using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using SignalRThroughputBench.Runner.Options;

namespace SignalRThroughputBench.Runner.Clients;

public static class SignalRClientFactory
{
    public static SignalRBenchClient Create(RunnerOptions options, string? userId = null)
    {
        var url = string.IsNullOrWhiteSpace(userId)
            ? options.ServerUrl
            : $"{options.ServerUrl}?userId={Uri.EscapeDataString(userId)}";

        var builder = new HubConnectionBuilder()
            .WithUrl(url, httpOptions =>
            {
                httpOptions.Transports = options.Transport switch
                {
                    BenchTransport.Auto => HttpTransportType.WebSockets | HttpTransportType.LongPolling | HttpTransportType.ServerSentEvents,
                    BenchTransport.WebSocket => HttpTransportType.WebSockets,
                    BenchTransport.LongPolling => HttpTransportType.LongPolling,
                    BenchTransport.ServerSentEvents => HttpTransportType.ServerSentEvents,
                    _ => HttpTransportType.WebSockets
                };
            });

        if (options.Protocol == BenchProtocol.MessagePack)
        {
            builder.AddMessagePackProtocol();
        }

        return new SignalRBenchClient(builder.Build());
    }
}
