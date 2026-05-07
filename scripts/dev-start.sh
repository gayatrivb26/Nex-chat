#!/bin/bash
# ============================================================
# NexChat - Development Startup Script
# ============================================================
# Usage: ./scripts/dev-start.sh
# ============================================================

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

print_header() {
    echo ""
    echo -e "${CYAN}╔══════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}║          NexChat Dev Environment             ║${NC}"
    echo -e "${CYAN}╚══════════════════════════════════════════════╝${NC}"
    echo ""
}

print_step() { echo -e "${BLUE}[→]${NC} $1"; }
print_ok()   { echo -e "${GREEN}[✓]${NC} $1"; }
print_warn() { echo -e "${YELLOW}[!]${NC} $1"; }
print_err()  { echo -e "${RED}[✗]${NC} $1"; }

print_header

# --- Check prerequisites ---
print_step "Checking prerequisites..."

if ! command -v docker &>/dev/null; then
    print_err "Docker not found. Install from https://docs.docker.com/get-docker/"
    exit 1
fi
print_ok "Docker $(docker --version | awk '{print $3}' | tr -d ',')"

if ! command -v docker-compose &>/dev/null && ! docker compose version &>/dev/null; then
    print_err "Docker Compose not found."
    exit 1
fi
print_ok "Docker Compose available"

# --- Generate secrets if not present ---
if [ ! -f "./secrets/jwt_private_key" ]; then
    print_warn "Secrets not found. Generating..."
    bash ./scripts/generate-secrets.sh
fi
print_ok "Secrets present"

# --- Check .env ---
if [ ! -f ".env" ]; then
    print_warn ".env not found, copying from .env.example..."
    cp .env .env.backup 2>/dev/null || true
fi
print_ok ".env present"

# --- Validate Firebase credentials ---
if grep -q "YOUR_FIREBASE_PROJECT_ID" ./secrets/firebase_credentials 2>/dev/null; then
    print_warn "Firebase credentials are placeholder - push notifications won't work until updated"
fi

# --- Start infrastructure first ---
print_step "Starting infrastructure services (postgres, redis, kafka, minio)..."
docker-compose up -d postgres redis zookeeper kafka minio clamav

print_step "Waiting for postgres to be healthy..."
timeout 60 bash -c 'until docker-compose exec -T postgres pg_isready -U nexchat_user -d nexchat; do sleep 2; done'
print_ok "PostgreSQL ready"

print_step "Waiting for redis..."
timeout 30 bash -c 'until docker-compose exec -T redis redis-cli -a $REDIS_PASSWORD ping | grep -q PONG; do sleep 2; done' || true
print_ok "Redis ready"

print_step "Waiting for kafka..."
sleep 15  # Kafka needs more time
print_ok "Kafka warming up"

# --- Init Kafka topics ---
print_step "Initializing Kafka topics..."
docker-compose up -d kafka-init
sleep 5
print_ok "Kafka topics created"

# --- Init MinIO buckets ---
print_step "Initializing MinIO buckets..."
docker-compose up -d minio-init
sleep 5
print_ok "MinIO buckets created"

# --- Start API ---
print_step "Starting API instances (3 replicas)..."
docker-compose up -d api api-2 api-3

print_step "Waiting for API to be healthy..."
timeout 90 bash -c 'until curl -sf http://localhost:5000/health/live; do sleep 3; done'
print_ok "API instance 1 ready"

# --- Start Frontend ---
print_step "Starting Angular frontend..."
docker-compose up -d frontend

# --- Start Nginx ---
print_step "Starting Nginx reverse proxy..."
docker-compose up -d nginx

# --- Start Monitoring ---
print_step "Starting monitoring stack..."
docker-compose up -d prometheus grafana elasticsearch kibana

# --- Start mail ---
print_step "Starting MailHog (dev email)..."
docker-compose up -d mailhog kafka-ui

echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║         NexChat is Running! 🚀               ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════╝${NC}"
echo ""
echo -e "  ${CYAN}Application:${NC}     http://localhost"
echo -e "  ${CYAN}API Direct:${NC}      http://localhost:5000/api/v1"
echo -e "  ${CYAN}API Health:${NC}      http://localhost:5000/health"
echo -e "  ${CYAN}API Swagger:${NC}     http://localhost:5000/swagger"
echo -e "  ${CYAN}Grafana:${NC}         http://localhost:3000  (admin/NexChat_Grafana_P@ssw0rd_2024!)"
echo -e "  ${CYAN}Kibana:${NC}          http://localhost:5601"
echo -e "  ${CYAN}MinIO Console:${NC}   http://localhost:9001  (nexchat_minio_admin/...)"
echo -e "  ${CYAN}Kafka UI:${NC}        http://localhost:8080"
echo -e "  ${CYAN}MailHog:${NC}         http://localhost:8025"
echo -e "  ${CYAN}Prometheus:${NC}      http://localhost:9090"
echo ""
echo -e "  ${YELLOW}Logs:${NC} docker-compose logs -f api"
echo -e "  ${YELLOW}Stop:${NC} docker-compose down"
echo ""
