using System.Text.Json;
using SignalRThroughputBench.Runner.Options;

namespace SignalRThroughputBench.Runner.Cli;

public static class RunCommand
{
    public static RunnerOptions Parse(string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "run", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Expected the first command to be 'run'.");
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < args.Length; index++)
        {
            var arg = args[index];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = arg[2..];
            if (key is "fail-on-threshold" or "verbose")
            {
                values[key] = "true";
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for option '{arg}'.");
            }

            values[key] = args[++index];
        }

        RunnerOptions options = values.TryGetValue("config", out var configFile) && !string.IsNullOrWhiteSpace(configFile)
            ? LoadConfig(configFile!)
            : new RunnerOptions();

        return options with
        {
            ServerUrl = GetString(values, "server-url", options.ServerUrl),
            Scenario = GetString(values, "scenario", options.Scenario),
            Connections = GetInt(values, "connections", options.Connections),
            DurationSeconds = GetInt(values, "duration", options.DurationSeconds),
            WarmupSeconds = GetInt(values, "warmup", options.WarmupSeconds),
            CooldownSeconds = GetInt(values, "cooldown", options.CooldownSeconds),
            PayloadBytes = GetInt(values, "payload-bytes", options.PayloadBytes),
            Protocol = GetProtocol(values, options.Protocol),
            Transport = GetTransport(values, options.Transport),
            SendRate = GetNullableInt(values, "send-rate", options.SendRate),
            Groups = GetInt(values, "groups", options.Groups),
            Targets = GetInt(values, "targets", options.Targets),
            ParallelConnect = GetInt(values, "parallel-connect", options.ParallelConnect),
            OutputDirectory = GetString(values, "output", options.OutputDirectory),
            ConfigFile = GetString(values, "config", options.ConfigFile),
            RunId = GetString(values, "run-id", options.RunId),
            ThresholdFile = GetString(values, "threshold-file", options.ThresholdFile),
            FailOnThreshold = GetBool(values, "fail-on-threshold", options.FailOnThreshold),
            Verbose = GetBool(values, "verbose", options.Verbose)
        };
    }

    private static RunnerOptions LoadConfig(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<RunnerOptions>(json, JsonSerializerOptions.Web) ?? new RunnerOptions();
    }

    private static string GetString(IDictionary<string, string?> values, string key, string? fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback ?? string.Empty;

    private static int GetInt(IDictionary<string, string?> values, string key, int fallback) =>
        values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

    private static int? GetNullableInt(IDictionary<string, string?> values, string key, int? fallback) =>
        values.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

    private static bool GetBool(IDictionary<string, string?> values, string key, bool fallback) =>
        values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;

    private static BenchProtocol GetProtocol(IDictionary<string, string?> values, BenchProtocol fallback)
    {
        if (!values.TryGetValue("protocol", out var value) || string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Equals("messagepack", StringComparison.OrdinalIgnoreCase) ? BenchProtocol.MessagePack : BenchProtocol.Json;
    }

    private static BenchTransport GetTransport(IDictionary<string, string?> values, BenchTransport fallback)
    {
        if (!values.TryGetValue("transport", out var value) || string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.ToLowerInvariant() switch
        {
            "auto" => BenchTransport.Auto,
            "websocket" => BenchTransport.WebSocket,
            "long-polling" => BenchTransport.LongPolling,
            "server-sent-events" => BenchTransport.ServerSentEvents,
            _ => fallback
        };
    }
}
