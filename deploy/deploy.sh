#!/usr/bin/env bash
# 源码构建 + 启动 MASA DCC（独立部署模式）
set -euo pipefail
cd "$(dirname "$0")"

if [ ! -f .env ]; then
  echo "缺少 .env 文件，请先基于 deploy/.env 配置好再执行" >&2
  exit 1
fi

if ! grep -qE '^STANDALONE_JWT_SECRET=.{16,}$' .env; then
  echo "[警告] STANDALONE_JWT_SECRET 过短，请改成足够长的随机字符串" >&2
fi

echo ">>> 1/2 构建镜像（首次较慢，需拉取 .NET SDK 基础镜像）"
docker compose build

echo ">>> 2/2 启动容器"
docker compose up -d

echo
docker compose ps
echo
echo "API:      http://<服务器IP>:$(grep -E '^DCC_SERVICE_PORT=' .env | cut -d= -f2)"
echo "管理后台: http://<服务器IP>:$(grep -E '^DCC_WEB_PORT=' .env | cut -d= -f2)"
echo "查看日志: ./logs.sh"
