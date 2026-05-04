$ErrorActionPreference = "Stop"
docker compose -f docker/compose.redis-scaleout.yml up --build -d
docker compose -f docker/compose.redis-scaleout.yml run --rm benchmark-runner run --server-url http://signalr-server-1:8080/bench --scenario group-broadcast --connections 20 --groups 4 --duration 5 --warmup 2 --protocol messagepack --transport websocket
