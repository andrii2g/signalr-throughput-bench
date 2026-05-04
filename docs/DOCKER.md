# Docker

- `compose.baseline.yml` runs a single server and optional runner.
- `compose.redis-scaleout.yml` runs Redis and two SignalR servers.
- The runner can execute inside Docker or from the host against Docker-hosted servers.
- CPU and memory limits will affect the observed throughput and latency.
