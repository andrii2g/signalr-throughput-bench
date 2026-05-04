using SignalRThroughputBench.Runner;
using SignalRThroughputBench.Runner.Cli;

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run --project src/SignalRThroughputBench.Runner -- run [options]");
    return 1;
}

var options = RunCommand.Parse(args);
var runner = new BenchmarkRunner();
return await runner.RunAsync(options, CancellationToken.None).ConfigureAwait(false);
