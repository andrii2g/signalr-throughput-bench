# Results Guide

- Compare p50, p95, and p99 together rather than using only one percentile.
- Rising p99 with flat p50 usually indicates queuing or intermittent contention.
- Missing deliveries and connection failures should be examined before trusting throughput numbers.
- MessagePack is expected to reduce payload size, but not every scenario benefits equally.
- Redis scale-out introduces propagation overhead.
