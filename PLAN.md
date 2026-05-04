# PLAN.md - SignalRThroughputBench

## 0. Purpose

Create a standalone .NET repository named `SignalRThroughputBench` that measures practical ASP.NET Core SignalR performance under repeatable local and Docker-based scenarios.

The repository must answer these questions:

1. How many messages per second can a SignalR hub deliver for common workloads?
2. What is the latency distribution under increasing connection counts?
3. How do JSON and MessagePack hub protocols compare?
4. How do WebSockets and Long Polling compare when forced explicitly?
5. What is the cost of broadcasting to all clients versus groups versus targeted users?
6. What changes when the server is scaled to multiple replicas with a Redis backplane?
7. How much CPU, memory, allocations, GC pressure, and network traffic are produced by each run?

This repository is not a generic load testing framework. It is a focused SignalR benchmark harness with deterministic scenarios, structured output, and Docker Compose profiles for repeatability.

## 1. Repository name

Recommended repository name:

```text
signalr-throughput-bench
```

Recommended root namespace:

```text
SignalRThroughputBench
```

Recommended solution name:

```text
SignalRThroughputBench.sln
```

## 2. Core design decision

Use Docker containers for benchmark execution, but keep local execution supported.

Rationale:

- Docker gives repeatable server and Redis topology.
- Docker Compose can model one server, multiple servers, Redis backplane, and optional reverse proxy profiles.
- Local execution is still useful for quick development and debugging.
- Client load generation can run either on the host or inside a container.

Final contract:

- The benchmark server must run as a normal ASP.NET Core application.
- The benchmark runner must run as a CLI application.
- Docker Compose must be the canonical repeatable environment.
- Local CLI scripts must be available for fast development.

## 3. Technology choices

Use:

- .NET 10 SDK if available in the development environment.
- C# latest language version.
- ASP.NET Core SignalR.
- Microsoft.AspNetCore.SignalR.Client.
- Microsoft.AspNetCore.SignalR.Protocols.MessagePack.
- Microsoft.AspNetCore.SignalR.StackExchangeRedis for Redis backplane scenarios.
- System.Diagnostics.Metrics for internal metrics.
- System.Diagnostics.Process for client-side process resource snapshots where available.
- Docker Compose for repeatable benchmark topologies.
- Redis container for scale-out backplane scenarios.
- Markdown, JSON, and CSV reports.

Do not use BenchmarkDotNet for the main SignalR load tests.

Reason:

BenchmarkDotNet is excellent for microbenchmarks, but this project benchmarks a distributed, asynchronous, networked system. The runner needs controlled warmup, many concurrent clients, transport negotiation, live counters, duration windows, and external service topology. BenchmarkDotNet may be added later only for isolated serialization or payload construction microbenchmarks.

## 4. Target scenarios

The repository must implement these scenarios in v1.

### 4.1 Echo round-trip

Each client invokes a hub method and waits for the server response.

Purpose:

- Measure request-response latency.
- Measure per-client round-trip throughput.
- Useful for RPC-style SignalR usage.

Hub method:

```csharp
Task<EchoResponse> Echo(EchoRequest request)
```

Metrics:

- completed calls
- failed calls
- calls per second
- p50, p75, p90, p95, p99 latency
- max latency
- client-side exceptions

### 4.2 Server broadcast to all

A coordinator invokes a hub method that broadcasts one message to all connected clients.

Purpose:

- Measure fan-out cost.
- Measure delivery latency across client population.

Hub method:

```csharp
Task Broadcast(BroadcastRequest request)
```

Server action:

```csharp
Clients.All.SendAsync("Receive", payload)
```

Metrics:

- messages requested
- total deliveries expected
- total deliveries observed
- missing deliveries
- delivery rate per second
- latency from server send timestamp to client receive timestamp

### 4.3 Group broadcast

Clients join groups and the coordinator broadcasts to selected groups.

Purpose:

- Measure group fan-out cost.
- Compare one large group versus many small groups.

Hub methods:

```csharp
Task JoinGroup(string groupName)
Task BroadcastGroup(GroupBroadcastRequest request)
```

Server action:

```csharp
Clients.Group(groupName).SendAsync("Receive", payload)
```

Metrics:

- group count
- clients per group
- expected deliveries
- observed deliveries
- latency percentiles
- throughput

### 4.4 Targeted user send

Clients connect with deterministic user identifiers. The coordinator sends to specific users.

Purpose:

- Measure `Clients.User(userId)` behavior.
- Validate targeted delivery and identity mapping.

Contract:

- The benchmark server must use a deterministic custom `IUserIdProvider`.
- The user id must be taken from a query string parameter named `userId`.
- If `userId` is missing, the connection id may be used only for development, not for benchmark reports.

Metrics:

- target count
- expected deliveries
- observed deliveries
- latency percentiles

### 4.5 Connection storm

The runner opens many connections as quickly as possible.

Purpose:

- Measure connection establishment rate.
- Detect server or client bottlenecks.

Metrics:

- attempted connections
- successful connections
- failed connections
- connects per second
- time to 50 percent connected
- time to 100 percent connected
- failure reasons grouped by type

### 4.6 Idle connection capacity smoke test

The runner opens many idle connections and keeps them alive for a fixed duration.

Purpose:

- Measure memory footprint and stability with many connected clients.
- Catch connection lifetime issues.

Metrics:

- connected clients
- disconnects
- reconnects
- server memory snapshots
- runner memory snapshots
- server heartbeat observations if available

## 5. Protocol and transport matrix

The runner must allow these options:

```text
--protocol json|messagepack
--transport websocket|long-polling|server-sent-events|auto
```

Initial v1 must fully support:

- JSON over WebSockets
- MessagePack over WebSockets
- JSON over Long Polling
- MessagePack over Long Polling

Server-Sent Events can be accepted as an option but may be marked experimental in v1 because support depends on environment and client behavior.

Important behavior:

- `auto` means SignalR transport negotiation is allowed.
- Explicit `websocket` means `HttpTransportType.WebSockets` only.
- Explicit `long-polling` means `HttpTransportType.LongPolling` only.
- The selected transport and negotiated transport must be written into `run-summary.json`.

## 6. Docker topology

Create Docker Compose files with profiles.

### 6.1 Baseline single server

Profile:

```text
baseline
```

Services:

```text
signalr-server
benchmark-runner optional
```

Purpose:

- One server instance.
- No Redis.
- Best baseline for comparing protocols, transports, and payload sizes.

### 6.2 Redis backplane scale-out

Profile:

```text
redis-scaleout
```

Services:

```text
signalr-server-1
signalr-server-2
redis
benchmark-runner optional
```

Purpose:

- Measure scale-out delivery behavior with Redis backplane.
- Validate group and broadcast delivery across more than one server.

Requirement:

- Use `Microsoft.AspNetCore.SignalR.StackExchangeRedis`.
- Redis must run in the same Docker network as the servers.
- Backplane must be enabled only when `SIGNALR_BACKPLANE=redis`.

### 6.3 Optional reverse proxy profile

Profile:

```text
proxy
```

Services:

```text
nginx
signalr-server-1
signalr-server-2
redis optional
```

Purpose:

- Later test proxy behavior, sticky sessions, and WebSocket forwarding.

v1 requirement:

- Include a placeholder `docker/nginx/nginx.conf` only if it works.
- If not implemented, document it as vNext and do not ship broken config.

## 7. Repository structure

Create this structure:

```text
/
  README.md
  PLAN.md
  LICENSE
  .gitignore
  .editorconfig
  Directory.Build.props
  Directory.Packages.props
  SignalRThroughputBench.sln

  src/
    SignalRThroughputBench.Server/
      SignalRThroughputBench.Server.csproj
      Program.cs
      Hubs/
        BenchHub.cs
      Models/
        EchoRequest.cs
        EchoResponse.cs
        BroadcastRequest.cs
        GroupBroadcastRequest.cs
        BenchPayload.cs
      Identity/
        QueryStringUserIdProvider.cs
      Metrics/
        ServerMetrics.cs
      Options/
        SignalRBenchServerOptions.cs

    SignalRThroughputBench.Runner/
      SignalRThroughputBench.Runner.csproj
      Program.cs
      Cli/
        RunCommand.cs
        ScenarioCommand.cs
        CompareCommand.cs
      Scenarios/
        IBenchScenario.cs
        EchoScenario.cs
        BroadcastAllScenario.cs
        GroupBroadcastScenario.cs
        TargetedUserScenario.cs
        ConnectionStormScenario.cs
        IdleConnectionsScenario.cs
      Clients/
        SignalRBenchClient.cs
        SignalRClientFactory.cs
      Load/
        ClientGroupPlanner.cs
        RateLimiter.cs
        WarmupController.cs
      Metrics/
        LatencyHistogram.cs
        RunMetrics.cs
        ResourceSampler.cs
      Reports/
        JsonReportWriter.cs
        CsvReportWriter.cs
        MarkdownReportWriter.cs
      Options/
        RunnerOptions.cs
        ScenarioOptions.cs

    SignalRThroughputBench.Contracts/
      SignalRThroughputBench.Contracts.csproj
      Payloads/
        EchoRequest.cs
        EchoResponse.cs
        BroadcastRequest.cs
        GroupBroadcastRequest.cs
        BenchPayload.cs
      Reports/
        RunSummary.cs
        ScenarioSummary.cs
        LatencySummary.cs
        ResourceSummary.cs

  tests/
    SignalRThroughputBench.Tests/
      SignalRThroughputBench.Tests.csproj
      LatencyHistogramTests.cs
      ReportWriterTests.cs
      ClientGroupPlannerTests.cs
      OptionsParsingTests.cs

  docker/
    Dockerfile.server
    Dockerfile.runner
    compose.baseline.yml
    compose.redis-scaleout.yml
    redis/
      redis.conf

  scripts/
    run-local-baseline.ps1
    run-local-baseline.sh
    run-docker-baseline.ps1
    run-docker-baseline.sh
    run-docker-redis-scaleout.ps1
    run-docker-redis-scaleout.sh

  docs/
    BENCHMARK_MODEL.md
    SCENARIOS.md
    REPORT_FORMAT.md
    DOCKER.md
    RESULTS_GUIDE.md
    TROUBLESHOOTING.md

  samples/
    configs/
      echo.websocket.json.json
      broadcast.messagepack.websocket.json
      groups.redis-scaleout.json

  results/
    .gitkeep
```

## 8. Project responsibilities

### 8.1 Contracts project

Contains shared DTOs used by the server and runner.

Rules:

- DTOs must be immutable records where practical.
- Keep DTOs serialization-friendly.
- Do not put server logic in this project.
- Do not put runner logic in this project.

### 8.2 Server project

Hosts the SignalR hub and minimal health endpoints.

Endpoints:

```text
GET /health/live
GET /health/ready
GET /metrics/snapshot
MapHub<BenchHub> /bench
```

`/health/live` returns 200 when the process is running.

`/health/ready` returns 200 when the app is ready to accept SignalR clients.

`/metrics/snapshot` returns a lightweight JSON snapshot with server counters.

### 8.3 Runner project

Runs benchmark scenarios and produces reports.

It must support:

- CLI options.
- JSON config files.
- deterministic run id.
- warmup phase.
- measurement phase.
- cooldown phase.
- structured reports.
- non-zero exit code on benchmark execution failure.
- optional non-zero exit code on threshold violations.

## 9. CLI contract

Use this command shape:

```text
dotnet run --project src/SignalRThroughputBench.Runner -- run [options]
```

Future global tool command name:

```text
signalrbench
```

### 9.1 Main run options

Required or defaulted options:

```text
--server-url <url>                 default: http://localhost:5080/bench
--scenario <name>                  required unless --config is provided
--connections <number>             default: 100
--duration <seconds>               default: 30
--warmup <seconds>                 default: 10
--cooldown <seconds>               default: 3
--payload-bytes <number>           default: 256
--protocol json|messagepack        default: json
--transport auto|websocket|long-polling|server-sent-events default: websocket
--send-rate <number>               messages per second, optional
--groups <number>                  default: 10 for group scenario
--targets <number>                 default: 100 for targeted scenario
--parallel-connect <number>        default: 50
--output <directory>               default: results/<timestamp>-<scenario>
--config <file>                    optional JSON config
--run-id <id>                      optional deterministic run id
--threshold-file <file>            optional thresholds JSON
--fail-on-threshold                default: false
--verbose                          default: false
```

Scenario names:

```text
echo
broadcast-all
group-broadcast
targeted-user
connection-storm
idle-connections
```

### 9.2 Compare command

Add a compare command if time permits:

```text
signalrbench compare --baseline <run-summary.json> --candidate <run-summary.json> --output <directory>
```

This command must compare:

- throughput delta
- p50 latency delta
- p95 latency delta
- p99 latency delta
- failure rate delta
- memory delta if available

If not implemented in v1, add it to README as planned functionality and do not document it as working.

## 10. Report contract

Each run must produce:

```text
run-summary.json
latency.csv
throughput.csv
resources.csv
report.md
raw-events.ndjson optional
```

### 10.1 run-summary.json

Required top-level shape:

```json
{
  "schemaVersion": 1,
  "runId": "2026-05-05T21-30-00Z-echo-json-websocket",
  "startedAtUtc": "2026-05-05T21:30:00Z",
  "endedAtUtc": "2026-05-05T21:30:43Z",
  "scenario": "echo",
  "serverUrl": "http://localhost:5080/bench",
  "protocol": "json",
  "transportRequested": "websocket",
  "transportObserved": "websocket",
  "connections": 100,
  "payloadBytes": 256,
  "warmupSeconds": 10,
  "durationSeconds": 30,
  "totalOperations": 123456,
  "failedOperations": 0,
  "operationsPerSecond": 4115.2,
  "latency": {
    "p50Ms": 2.1,
    "p75Ms": 3.4,
    "p90Ms": 5.9,
    "p95Ms": 8.3,
    "p99Ms": 18.5,
    "maxMs": 120.4
  },
  "resources": {
    "runnerMaxWorkingSetMb": 512.2,
    "serverMaxWorkingSetMb": null,
    "runnerCpuPercentAvg": 73.2,
    "serverCpuPercentAvg": null
  },
  "environment": {
    "dotnetVersion": "10.0.x",
    "os": "Linux",
    "containerized": true,
    "processorCount": 8
  },
  "thresholds": {
    "evaluated": false,
    "passed": null,
    "violations": []
  }
}
```

### 10.2 latency.csv

Required columns:

```text
elapsed_ms,operation,latency_ms,success,error_type
```

### 10.3 throughput.csv

Required columns:

```text
elapsed_second,operations,failures,operations_per_second
```

### 10.4 resources.csv

Required columns:

```text
elapsed_second,process,working_set_mb,private_memory_mb,cpu_percent,thread_count,gc_heap_mb,gen0,gen1,gen2
```

If a metric is unavailable, write an empty field, not `0`.

### 10.5 report.md

Must include:

- run configuration
- environment
- summary table
- latency percentiles
- throughput summary
- failures
- threshold result if applicable
- interpretation notes

## 11. Measurement rules

### 11.1 Warmup

Warmup must:

- establish connections
- run the same scenario pattern
- not count operations in final metrics
- log failures separately

### 11.2 Measurement

Measurement starts after warmup and ends after the configured duration.

Rules:

- Use monotonic timing via `Stopwatch`.
- Record client-side timestamps for every measured operation.
- For delivery scenarios, use message ids to correlate sends and receives.
- Do not use wall clock time for latency calculations except for report metadata.

### 11.3 Cooldown

Cooldown must:

- stop new sends
- wait for late deliveries for the configured cooldown duration
- close connections gracefully
- record missed deliveries

### 11.4 Latency histogram

Implement an internal latency collector.

Requirement:

- It must not store unbounded data for very long runs.
- For v1, storing all latencies is acceptable up to a documented safety cap.
- Default safety cap: 5,000,000 latency samples.
- If the cap is exceeded, switch to reservoir sampling or bucketed histogram and mark `latencySamplingMode` in `run-summary.json`.

## 12. Payload model

Payloads must be deterministic.

`BenchPayload` fields:

```csharp
public sealed record BenchPayload(
    string MessageId,
    int Sequence,
    long CreatedAtStopwatchTicks,
    int PayloadBytes,
    string Data);
```

Rules:

- `Data` must be generated deterministically from payload size.
- Use repeated ASCII characters for predictable payload size.
- Payload size means approximate serialized payload body size, not exact wire size.
- Record the requested payload size in all reports.

## 13. Server implementation details

### 13.1 Program.cs

Must support environment variables:

```text
ASPNETCORE_URLS=http://+:8080
SIGNALR_PROTOCOLS=json,messagepack
SIGNALR_BACKPLANE=none|redis
SIGNALR_REDIS_CONNECTION=redis:6379
SIGNALR_ENABLE_DETAILED_ERRORS=false
SIGNALR_MAX_RECEIVE_MESSAGE_SIZE_BYTES=32768
```

Behavior:

- JSON is always enabled.
- MessagePack is enabled when package is referenced and `SIGNALR_PROTOCOLS` contains `messagepack`.
- Redis backplane is enabled only when `SIGNALR_BACKPLANE=redis`.
- If Redis is requested but connection string is missing, fail startup with a clear error.

### 13.2 Hub methods

Implement:

```csharp
Task<EchoResponse> Echo(EchoRequest request);
Task JoinGroup(string groupName);
Task LeaveGroup(string groupName);
Task Broadcast(BroadcastRequest request);
Task BroadcastGroup(GroupBroadcastRequest request);
Task SendToUser(TargetedUserRequest request);
Task Ping();
```

Rules:

- Hub methods must avoid unnecessary allocations.
- Hub methods must not write logs per message by default.
- Per-message debug logging can exist only behind a disabled option.

## 14. Runner implementation details

### 14.1 Client factory

The factory must configure:

- hub URL
- requested protocol
- requested transport
- automatic reconnect disabled by default for benchmarks
- automatic reconnect optional for idle smoke tests
- user id query string

Protocol behavior:

- JSON client uses default protocol.
- MessagePack client calls `.AddMessagePackProtocol()`.

Transport behavior:

- For explicit transport, set `HttpTransportType` exactly.
- For auto, let SignalR negotiate.

### 14.2 Connection management

Connection creation must be bounded by `--parallel-connect`.

Rules:

- Do not start all client connections with unbounded `Task.WhenAll`.
- Record individual connection failures.
- Stop the run if successful connections are below 95 percent of requested connections, unless `--allow-partial-connections` is added later.

### 14.3 Failure classification

Classify failures into:

```text
connection_failed
connection_dropped
send_failed
receive_timeout
protocol_error
transport_error
server_error
threshold_violation
unknown
```

Each report must include grouped failure counts.

## 15. Threshold contract

Threshold file example:

```json
{
  "schemaVersion": 1,
  "rules": [
    {
      "metric": "operationsPerSecond",
      "operator": ">=",
      "value": 10000
    },
    {
      "metric": "latency.p95Ms",
      "operator": "<=",
      "value": 50
    },
    {
      "metric": "failedOperations",
      "operator": "==",
      "value": 0
    }
  ]
}
```

Supported operators:

```text
< <= > >= == !=
```

Behavior:

- If threshold file is provided, always evaluate it.
- If `--fail-on-threshold` is false, threshold failures are reported but process exits 0 if the benchmark itself completed.
- If `--fail-on-threshold` is true, threshold failures make the process exit non-zero.

## 16. Docker files

### 16.1 Server Dockerfile

Use multi-stage build.

Requirements:

- build stage with .NET SDK
- runtime stage with ASP.NET runtime
- expose 8080
- set `ASPNETCORE_URLS=http://+:8080`
- run as non-root if practical

### 16.2 Runner Dockerfile

Use multi-stage build.

Requirements:

- build stage with .NET SDK
- runtime stage with .NET runtime
- entrypoint to runner DLL
- allow arguments to be passed through

### 16.3 Baseline Compose

`docker/compose.baseline.yml` must start the server.

Example command:

```bash
docker compose -f docker/compose.baseline.yml up --build signalr-server
```

Runner example:

```bash
docker compose -f docker/compose.baseline.yml run --rm benchmark-runner run --server-url http://signalr-server:8080/bench --scenario echo --connections 100 --duration 30 --protocol json --transport websocket
```

### 16.4 Redis scale-out Compose

`docker/compose.redis-scaleout.yml` must start:

- redis
- signalr-server-1
- signalr-server-2
- optional runner

If no reverse proxy is implemented, the runner must target one server directly for v1 and documentation must explain the limitation.

Preferred v1 implementation:

- Add nginx only if sticky and WebSocket configuration are correct.
- Otherwise keep scale-out focused on Redis propagation validation rather than load balancing.

## 17. README.md contract

Root README must be compact and practical.

Required sections:

1. What this is
2. Why Docker is used
3. Quick start local
4. Quick start Docker baseline
5. Quick start Redis scale-out
6. Example reports
7. Supported scenarios
8. Supported protocols and transports
9. Known limitations

README must not claim production-grade benchmark accuracy. It must explain that results depend on hardware, OS, container limits, network, CPU throttling, and client/server placement.

## 18. Documentation files

### 18.1 docs/BENCHMARK_MODEL.md

Explain:

- warmup
- measurement
- cooldown
- latency model
- delivery correlation
- throughput model
- resource sampling

### 18.2 docs/SCENARIOS.md

Document each scenario with:

- purpose
- command example
- metrics
- interpretation
- common pitfalls

### 18.3 docs/REPORT_FORMAT.md

Define JSON, CSV, Markdown outputs.

### 18.4 docs/DOCKER.md

Explain:

- baseline compose
- redis compose
- containerized runner
- host runner against Docker server
- CPU/memory limit notes

### 18.5 docs/RESULTS_GUIDE.md

Explain how to interpret:

- p50 vs p95 vs p99
- throughput saturation
- missing deliveries
- connection failures
- JSON vs MessagePack differences
- WebSocket vs Long Polling differences
- Redis backplane overhead

### 18.6 docs/TROUBLESHOOTING.md

Include:

- WebSocket connection failures
- Docker DNS issues
- Redis connection failures
- too many open files
- ephemeral port exhaustion
- CPU throttling
- high p99 latency
- missing group messages

## 19. Tests

Implement unit tests for:

- latency percentile calculation
- empty latency handling
- CSV escaping
- JSON report schema basics
- threshold evaluation
- CLI option parsing
- group planner distribution
- payload generation size behavior

Do not write flaky integration load tests as normal unit tests.

Add optional integration tests behind a test category:

```text
Category=Integration
```

Integration tests may:

- start TestServer or WebApplicationFactory
- connect a small number of SignalR clients
- validate echo
- validate broadcast
- validate group delivery

## 20. GitHub Actions

Create CI workflow:

```text
.github/workflows/ci.yml
```

Requirements:

- checkout
- setup dotnet
- restore
- build Release
- test Release
- optionally build Docker images

Do not run heavy benchmarks in PR CI by default.

Optional workflow:

```text
.github/workflows/smoke-benchmark.yml
```

Manual trigger only:

```yaml
on:
  workflow_dispatch:
```

It may run a tiny benchmark:

```text
connections=10 duration=5 warmup=2
```

## 21. Acceptance criteria

The implementation is complete when all items are true:

1. `dotnet build` succeeds.
2. `dotnet test` succeeds.
3. Server starts locally and exposes `/health/ready`.
4. Runner can execute `echo` with 10 clients locally.
5. Runner can execute `broadcast-all` with 10 clients locally.
6. Runner can execute `group-broadcast` with 10 clients locally.
7. Runner writes `run-summary.json`, `latency.csv`, `throughput.csv`, `resources.csv`, and `report.md`.
8. Docker baseline server starts successfully.
9. Docker runner can execute at least one baseline scenario against Docker server.
10. Redis scale-out profile starts Redis and two SignalR server containers.
11. Redis backplane is enabled only when configured.
12. README quick start commands work as written.
13. No per-message logs are emitted during normal benchmark runs.
14. Reports clearly identify protocol, transport, scenario, payload size, connection count, and duration.

## 22. Initial implementation order

### Phase 1 - Skeleton

1. Create solution and projects.
2. Add shared contracts project.
3. Add server project with health endpoints.
4. Add runner project with CLI skeleton.
5. Add tests project.
6. Add Directory.Build.props and Directory.Packages.props.

### Phase 2 - Minimal SignalR path

1. Implement `BenchHub`.
2. Implement `Echo` method.
3. Implement runner connection factory.
4. Implement echo scenario.
5. Implement JSON report writer.
6. Validate local 1-client run.

### Phase 3 - Real benchmark loop

1. Add warmup, measurement, cooldown.
2. Add latency collector.
3. Add throughput buckets.
4. Add Markdown and CSV reports.
5. Add failure classification.
6. Validate local 100-client run.

### Phase 4 - Fan-out scenarios

1. Add broadcast-all.
2. Add group broadcast.
3. Add targeted user send.
4. Add delivery correlation.
5. Add missed delivery reporting.

### Phase 5 - Protocol and transport matrix

1. Add MessagePack package to server and runner.
2. Add protocol option.
3. Add transport option.
4. Validate JSON WebSocket.
5. Validate MessagePack WebSocket.
6. Validate JSON Long Polling.
7. Validate MessagePack Long Polling.

### Phase 6 - Docker

1. Add server Dockerfile.
2. Add runner Dockerfile.
3. Add baseline compose.
4. Add scripts for baseline runs.
5. Validate Docker baseline scenario.

### Phase 7 - Redis scale-out

1. Add Redis package.
2. Add environment-driven Redis backplane configuration.
3. Add Redis compose file.
4. Validate two server instances start.
5. Validate basic delivery when Redis is enabled.
6. Document scale-out limitations clearly.

### Phase 8 - Polish

1. Add docs.
2. Add CI.
3. Add sample configs.
4. Review README commands.
5. Ensure no generated benchmark output is committed except `.gitkeep`.

## 23. Coding standards

- Use file-scoped namespaces.
- Use nullable reference types.
- Use `ConfigureAwait(false)` in library-like code where appropriate, but not obsessively in ASP.NET Core request paths.
- Prefer records for DTOs.
- Avoid static global mutable state except for metrics primitives.
- Avoid per-message console output.
- Use structured logging for lifecycle events only.
- Keep scenario implementations isolated.
- Keep report schemas stable.

## 24. Git ignore rules

Ensure `.gitignore` excludes:

```text
bin/
obj/
.vs/
.idea/
results/*
!results/.gitkeep
*.user
*.suo
TestResults/
```

## 25. Known limitations for v1

Document these clearly:

- Localhost benchmarks are not production benchmarks.
- Docker networking changes absolute latency values.
- Running server and runner on the same machine creates CPU contention.
- Long Polling is expected to behave differently from WebSockets.
- Redis backplane adds propagation overhead and must be close to servers for meaningful production-like results.
- Browser clients are not tested in v1.
- Azure SignalR Service is not tested in v1.

## 26. vNext ideas

Possible future extensions:

1. Browser client benchmark using Playwright.
2. Nginx reverse proxy and sticky session profile.
3. Azure SignalR Service mode.
4. Prometheus metrics export.
5. Grafana dashboard.
6. k6 or NBomber comparison adapter.
7. GitHub Actions performance trend publishing.
8. HTML report with charts.
9. OpenTelemetry traces.
10. Linux `pidstat` and `ss` integration.
11. Server GC versus workstation GC comparison.
12. HTTP/2 and HTTP/3 experiments where SignalR transport support allows it.

## 27. First useful benchmark commands

Local server:

```bash
dotnet run --project src/SignalRThroughputBench.Server --configuration Release
```

Local echo benchmark:

```bash
dotnet run --project src/SignalRThroughputBench.Runner --configuration Release -- run --server-url http://localhost:5080/bench --scenario echo --connections 100 --duration 30 --warmup 10 --payload-bytes 256 --protocol json --transport websocket
```

MessagePack comparison:

```bash
dotnet run --project src/SignalRThroughputBench.Runner --configuration Release -- run --server-url http://localhost:5080/bench --scenario echo --connections 100 --duration 30 --warmup 10 --payload-bytes 256 --protocol messagepack --transport websocket
```

Broadcast test:

```bash
dotnet run --project src/SignalRThroughputBench.Runner --configuration Release -- run --server-url http://localhost:5080/bench --scenario broadcast-all --connections 500 --duration 30 --warmup 10 --payload-bytes 256 --protocol json --transport websocket
```

Docker baseline:

```bash
docker compose -f docker/compose.baseline.yml up --build -d signalr-server

docker compose -f docker/compose.baseline.yml run --rm benchmark-runner run --server-url http://signalr-server:8080/bench --scenario echo --connections 100 --duration 30 --warmup 10 --protocol json --transport websocket
```

Redis scale-out:

```bash
docker compose -f docker/compose.redis-scaleout.yml up --build -d

docker compose -f docker/compose.redis-scaleout.yml run --rm benchmark-runner run --server-url http://signalr-server-1:8080/bench --scenario group-broadcast --connections 200 --groups 20 --duration 30 --warmup 10 --protocol messagepack --transport websocket
```

## 28. Final note for Codex

Implement the smallest working vertical slice first:

1. Server health endpoint.
2. SignalR hub with Echo.
3. Runner with 1-client echo.
4. JSON report.
5. Then scale to many clients.

Do not start with Docker, Redis, reports, and all scenarios at once. The first checkpoint must prove that one SignalR client can connect, call Echo, and write a valid `run-summary.json`.
