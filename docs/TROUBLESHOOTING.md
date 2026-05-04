# Troubleshooting

- WebSocket connection failures: verify server URL and transport selection.
- Docker DNS issues: verify the compose service name used in `--server-url`.
- Redis connection failures: check `SIGNALR_REDIS_CONNECTION`.
- Ephemeral port exhaustion: reduce client count or pace connection creation.
- High p99 latency: check CPU contention and container resource limits.
