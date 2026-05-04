#!/usr/bin/env bash
set -euo pipefail
dotnet run --project src/SignalRThroughputBench.Server --configuration Release
