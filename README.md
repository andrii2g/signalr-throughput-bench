# SignalRThroughputBench

SignalR throughput and latency benchmark harness for ASP.NET Core SignalR on `.NET 10`.

## What This Is

This repository measures practical SignalR behavior for local and Docker-based scenarios, including echo, broadcast, groups, targeted users, connection storms, idle connections, MessagePack, Long Polling, and Redis backplane scale-out.

## Why Docker Is Used

Docker Compose is the canonical repeatable environment for baseline and Redis scale-out runs. Local execution is still supported for fast iteration.

## Quick Start Local

Start the server in one terminal and keep it running:

```bash
dotnet restore SignalRThroughputBench.slnx
dotnet build SignalRThroughputBench.slnx --no-restore
ASPNETCORE_URLS=http://0.0.0.0:5080 dotnet run --project src/SignalRThroughputBench.Server --configuration Release --no-launch-profile
```

Verify the server is listening before starting the runner:

```bash
curl http://localhost:5080/health/ready
```

Expected response:

```json
{"status":"ready"}
```

Then run the benchmark from a second terminal:

```bash
dotnet run --project src/SignalRThroughputBench.Runner --configuration Release -- run --server-url http://localhost:5080/bench --scenario echo --connections 10 --duration 5 --warmup 2 --payload-bytes 256 --protocol json --transport websocket
```

If you are running inside WSL or Linux, start both the server and the runner in the same environment.

## Quick Start Docker Baseline

```bash
docker compose -f docker/compose.baseline.yml up --build -d signalr-server
docker compose -f docker/compose.baseline.yml run --rm benchmark-runner run --server-url http://signalr-server:8080/bench --scenario echo --connections 10 --duration 5 --warmup 2 --protocol json --transport websocket
```

The runner writes reports to the host `results/` directory. After the run completes, inspect the newest folder under:

```bash
ls results
```

## Quick Start Redis Scale-out

```bash
docker compose -f docker/compose.redis-scaleout.yml up --build -d
docker compose -f docker/compose.redis-scaleout.yml run --rm benchmark-runner run --server-url http://signalr-server-1:8080/bench --scenario group-broadcast --connections 20 --groups 4 --duration 5 --warmup 2 --protocol messagepack --transport websocket
```

Redis scale-out runs also persist reports into the host `results/` directory.

## Docker Cleanup

Stop and remove the baseline containers and network:

```bash
docker compose -f docker/compose.baseline.yml down
```

Stop and remove the Redis scale-out containers and network:

```bash
docker compose -f docker/compose.redis-scaleout.yml down
```

If you also want to remove anonymous volumes created by the compose run:

```bash
docker compose -f docker/compose.baseline.yml down -v
docker compose -f docker/compose.redis-scaleout.yml down -v
```

If you want to remove the built images too:

```bash
docker compose -f docker/compose.baseline.yml down --rmi local
docker compose -f docker/compose.redis-scaleout.yml down --rmi local
```

## Example Reports

Each run writes:

- `run-summary.json`
- `latency.csv`
- `throughput.csv`
- `resources.csv`
- `report.md`

## Supported Scenarios

- `echo`
- `broadcast-all`
- `group-broadcast`
- `targeted-user`
- `connection-storm`
- `idle-connections`

## Supported Protocols And Transports

- Protocols: JSON, MessagePack
- Transports: WebSockets, Long Polling, Server-Sent Events, Auto

## Known Limitations

- These are not production-grade measurements.
- Results depend on hardware, OS, container limits, CPU throttling, and client/server placement.
- Docker networking changes absolute latency values.
- Redis backplane runs are useful for comparison, not as a perfect production simulation.
