using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using SignalRThroughputBench.Server.Hubs;
using SignalRThroughputBench.Server.Identity;
using SignalRThroughputBench.Server.Metrics;
using SignalRThroughputBench.Server.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ServerMetrics>();
builder.Services.AddSingleton<IUserIdProvider, QueryStringUserIdProvider>();

var protocols = (Environment.GetEnvironmentVariable("SIGNALR_PROTOCOLS") ?? "json")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var backplane = Environment.GetEnvironmentVariable("SIGNALR_BACKPLANE") ?? "none";
var redisConnection = Environment.GetEnvironmentVariable("SIGNALR_REDIS_CONNECTION");
var detailedErrors = bool.TryParse(Environment.GetEnvironmentVariable("SIGNALR_ENABLE_DETAILED_ERRORS"), out var parsedDetailedErrors)
    && parsedDetailedErrors;
var maxReceiveSize = long.TryParse(Environment.GetEnvironmentVariable("SIGNALR_MAX_RECEIVE_MESSAGE_SIZE_BYTES"), out var parsedMaxReceiveSize)
    ? parsedMaxReceiveSize
    : 32_768L;

builder.Services.AddSingleton(new SignalRBenchServerOptions
{
    Protocols = protocols,
    Backplane = backplane,
    RedisConnection = redisConnection,
    EnableDetailedErrors = detailedErrors,
    MaxReceiveMessageSizeBytes = maxReceiveSize
});

var signalR = builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = detailedErrors;
    options.MaximumReceiveMessageSize = maxReceiveSize;
});

if (protocols.Any(static p => string.Equals(p, "messagepack", StringComparison.OrdinalIgnoreCase)))
{
    signalR.AddMessagePackProtocol();
}

if (string.Equals(backplane, "redis", StringComparison.OrdinalIgnoreCase))
{
    if (string.IsNullOrWhiteSpace(redisConnection))
    {
        throw new InvalidOperationException("SIGNALR_REDIS_CONNECTION must be provided when SIGNALR_BACKPLANE=redis.");
    }

    signalR.AddStackExchangeRedis(redisConnection);
}

var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));
app.MapGet("/metrics/snapshot", (ServerMetrics metrics) => Results.Ok(metrics.CreateSnapshot()));
app.MapHub<BenchHub>("/bench", options =>
{
    options.Transports = HttpTransportType.WebSockets |
                         HttpTransportType.LongPolling |
                         HttpTransportType.ServerSentEvents;
});

app.Run();

public partial class Program;
