# Scenarios

## echo
- Purpose: measure round-trip latency and throughput.
- Example: `signalrbench run --scenario echo`

## broadcast-all
- Purpose: measure fan-out to all connected clients.

## group-broadcast
- Purpose: measure broadcast cost scoped to groups.

## targeted-user
- Purpose: validate `Clients.User` delivery with deterministic user ids.

## connection-storm
- Purpose: measure connection establishment rate.

## idle-connections
- Purpose: keep many clients connected and observe resource usage.
