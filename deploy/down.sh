#!/usr/bin/env bash
# 停止并移除本编排的容器（不会动 MySQL / Redis）
set -euo pipefail
cd "$(dirname "$0")"
docker compose down
