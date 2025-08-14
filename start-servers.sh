#!/bin/bash
# start-servers.sh - Boilerplate for starting required servers for TriloBot.NET

set -euo pipefail

echo "Starting SignalR server..."
dotnet run --project TriloBot.Blazor

echo "Starting MediaMTX for webcam feed..."
cd _thirdparty/webrtc && ./mediamtx &

echo "All servers started."
