#!/usr/bin/env bash
# ============================================================
# MASA DCC 运行/维护脚本（不构建、不推送，只拉镜像并操作编排）
#
# 用法：
#   ./run.sh            登录 + 拉取镜像 + 启动编排（默认）
#   ./run.sh pull       只拉取镜像
#   ./run.sh up         只启动（不 pull）
#   ./run.sh down       停止并移除容器
#   ./run.sh restart    重启容器
#   ./run.sh ps         查看容器状态
#   ./run.sh logs [服务] 跟随日志，默认 masa-dcc-service
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${SCRIPT_DIR}"

log() { printf '\033[32m[%s] %s\033[0m\n' "$(date +%H:%M:%S)" "$*"; }
die() { printf '\033[31m[ERROR] %s\033[0m\n' "$*" >&2; exit 1; }

[ -f .env ] || die "缺少 .env（需与本脚本同目录）"
set -a; . ./.env; set +a

: "${REGISTRY:?}"; : "${REGISTRY_USER:?}"; : "${REGISTRY_PASSWORD:?}"; : "${IMAGE_TAG:?}"
export REGISTRY REGISTRY_NAMESPACE IMAGE_TAG

login() {
  log "登录镜像仓库 ${REGISTRY}"
  echo "${REGISTRY_PASSWORD}" | docker login "${REGISTRY}" -u "${REGISTRY_USER}" --password-stdin
}

case "${1:-up}" in
  pull)
    login
    docker compose pull
    ;;
  up)
    login
    docker compose pull
    docker compose up -d --remove-orphans
    docker compose ps
    ;;
  down)
    docker compose down
    ;;
  restart)
    docker compose restart
    ;;
  ps)
    docker compose ps
    ;;
  logs)
    shift || true
    if [ "$#" -eq 0 ]; then set -- masa-dcc-service; fi
    docker compose logs -f --tail=200 "$@"
    ;;
  *)
    echo "用法: ./run.sh [up|pull|down|restart|ps|logs [服务名]]"
    exit 1
    ;;
esac
