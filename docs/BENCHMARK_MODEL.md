# Benchmark Model

- Warmup establishes connections and exercises the scenario without contributing to final metrics.
- Measurement uses `Stopwatch` and runner-local timestamps only.
- Cooldown stops new sends and waits for late deliveries.
- Delivery correlation uses `MessageId`.
- Throughput is bucketed by elapsed second.
- Resource sampling records runner and server snapshots once per second where possible.
