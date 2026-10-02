#!/usr/bin/env bash
# 跟踪查看容器日志，默认跟随 dcc 服务端
set -euo pipefail
cd "$(dirname "$0")"
docker compose logs -f --tail=200 "${1:-masa-dcc-service}"
