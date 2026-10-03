#!/usr/bin/env bash
# ============================================================
# MASA DCC 一键部署脚本（在 Linux Docker 服务器上执行）
#
# 流程：拉取源码(SSH) -> 构建镜像 -> 推送镜像仓库 -> 启动编排
# 依赖：git、docker、docker-compose（v2）
#
# 用法：
#   ./deploy.sh              完整流程
#   ./deploy.sh --no-push    只构建，不推送
#   ./deploy.sh --no-build   不构建，直接推送已有镜像并启动
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "${SCRIPT_DIR}"

log()  { printf '\033[32m[%s] %s\033[0m\n' "$(date +%H:%M:%S)" "$*"; }
warn() { printf '\033[33m[WARN] %s\033[0m\n' "$*" >&2; }
die()  { printf '\033[31m[ERROR] %s\033[0m\n' "$*" >&2; exit 1; }

SKIP_BUILD=0
SKIP_PUSH=0
for arg in "$@"; do
  case "${arg}" in
    --no-build) SKIP_BUILD=1 ;;
    --no-push)  SKIP_PUSH=1 ;;
    -h|--help)  echo "用法: ./deploy.sh [--no-build] [--no-push]"; exit 0 ;;
    *)          echo "未知参数: ${arg}"; exit 1 ;;
  esac
done

# ---------- 0. 配置与依赖 ----------
[ -f .env ] || die "缺少 .env（需与本脚本同目录）"
set -a; . ./.env; set +a

: "${GIT_URL:?}"; : "${GIT_BRANCH:?}"; : "${SRC_DIR:?}"
: "${REGISTRY:?}"; : "${REGISTRY_NAMESPACE:?}"
: "${REGISTRY_USER:?}"; : "${REGISTRY_PASSWORD:?}"; : "${IMAGE_TAG:?}"
: "${INFRA_NETWORK:?}"

SERVICE_IMAGE="${REGISTRY}/${REGISTRY_NAMESPACE}/masa-dcc-service:${IMAGE_TAG}"
WEB_IMAGE="${REGISTRY}/${REGISTRY_NAMESPACE}/masa-dcc-web:${IMAGE_TAG}"

command -v git            >/dev/null 2>&1 || die "未安装 git"
command -v docker         >/dev/null 2>&1 || die "未安装 docker"
command -v docker-compose >/dev/null 2>&1 || die "未安装 docker-compose"
docker-compose version 2>/dev/null | grep -q "version v2" \
  || die "docker-compose 不是 v2（v1 不支持本编排文件），请升级到 Compose v2"

docker network inspect "${INFRA_NETWORK}" >/dev/null 2>&1 \
  || die "外部网络 ${INFRA_NETWORK} 不存在。请用 'docker network ls' 确认后修改 .env 的 INFRA_NETWORK"
docker network inspect "${INFRA_NETWORK}" -f '{{range .Containers}}{{.Name}} {{end}}' \
  | grep -q "${REDIS_HOST}" || warn "网络 ${INFRA_NETWORK} 内未见名为 ${REDIS_HOST} 的容器，请确认 REDIS_HOST/REDIS_PORT 是否正确"

case "${STANDALONE_JWT_SECRET}" in
  please-change*) warn "STANDALONE_JWT_SECRET 仍是默认值，生产环境请改成随机串" ;;
esac

# ---------- 1. 拉取源码 ----------
log "拉取源码 [${GIT_BRANCH}] <- ${GIT_URL}"
if [ -d "${SRC_DIR}/.git" ]; then
  git -C "${SRC_DIR}" fetch --prune origin
  git -C "${SRC_DIR}" checkout "${GIT_BRANCH}"
  git -C "${SRC_DIR}" reset --hard "origin/${GIT_BRANCH}"
else
  mkdir -p "$(dirname "${SRC_DIR}")"
  git clone --branch "${GIT_BRANCH}" "${GIT_URL}" "${SRC_DIR}"
fi
log "当前提交: $(git -C "${SRC_DIR}" log -1 --oneline)"

# ---------- 2. 构建镜像 ----------
if [ "${SKIP_BUILD}" -eq 0 ]; then
  log "构建 ${SERVICE_IMAGE}"
  docker build -t "${SERVICE_IMAGE}" \
    -f "${SRC_DIR}/src/Services/Masa.Dcc.Service/Dockerfile" "${SRC_DIR}"

  log "构建 ${WEB_IMAGE}"
  docker build -t "${WEB_IMAGE}" \
    -f "${SRC_DIR}/src/Web/Masa.Dcc.Web.Admin/Masa.Dcc.Web.Admin.Server/Dockerfile" "${SRC_DIR}"
else
  log "跳过构建（--no-build）"
fi

# ---------- 3. 推送镜像 ----------
if [ "${SKIP_PUSH}" -eq 0 ]; then
  log "登录镜像仓库 ${REGISTRY}"
  echo "${REGISTRY_PASSWORD}" | docker login "${REGISTRY}" -u "${REGISTRY_USER}" --password-stdin
  log "推送 ${SERVICE_IMAGE}"; docker push "${SERVICE_IMAGE}"
  log "推送 ${WEB_IMAGE}";    docker push "${WEB_IMAGE}"
else
  log "跳过推送（--no-push）"
fi

# ---------- 4. 启动编排 ----------
export REGISTRY REGISTRY_NAMESPACE IMAGE_TAG
log "启动编排（镜像标签 ${IMAGE_TAG}）"
docker-compose up -d --remove-orphans
docker-compose ps

echo
log "部署完成"
echo "  API      : http://<服务器IP>:${DCC_SERVICE_PORT}"
echo "  管理后台 : http://<服务器IP>:${DCC_WEB_PORT}"
echo "  查看日志 : ./run.sh logs masa-dcc-service"
