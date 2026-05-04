#!/usr/bin/env bash
set -euo pipefail
docker compose -f docker/compose.baseline.yml up --build -d signalr-server
docker compose -f docker/compose.baseline.yml run --rm benchmark-runner run --server-url http://signalr-server:8080/bench --scenario echo --connections 10 --duration 5 --warmup 2 --protocol json --transport websocket
