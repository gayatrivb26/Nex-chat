# NexChat — Production Real-Time Chat Application

> WhatsApp-scale chat built with .NET 8, Angular 17, SignalR, Kafka, PostgreSQL, Redis, and Docker.

---

## 🏗️ Architecture

```
Clients (Angular PWA / Electron Desktop)
        ↓ HTTPS / WSS
    Nginx (Reverse Proxy + Load Balancer)
        ↓
  .NET 8 API × 3 replicas
  ├── SignalR Hub (WebSocket real-time)
  ├── REST Controllers (MediatR + CQRS)
  └── Hangfire (Background jobs)
        ↓
  ┌─────────────────────────────────┐
  │  PostgreSQL 16  │  Redis 7      │
  │  (Primary DB)   │  (Cache+WS)   │
  ├─────────────────┼───────────────┤
  │  Apache Kafka   │  MinIO        │
  │  (Events)       │  (Files)      │
  └─────────────────┴───────────────┘
        ↓ Kafka Consumers
  ├── DeliveryReceiptConsumer
  ├── PushNotificationConsumer (FCM)
  ├── PresenceConsumer
  └── AnalyticsConsumer
```

---

## 🚀 Quick Start

### Prerequisites
- Docker 24+ and Docker Compose v2
- `openssl` (for key generation)
- 8GB RAM minimum for full stack

### 1. Clone and setup
```bash
git clone <your-repo>
cd nexchat

# Generate all secrets and keys
chmod +x scripts/*.sh
./scripts/generate-secrets.sh
```

### 2. Configure Firebase
Replace `secrets/firebase_credentials` with your real Firebase Admin SDK JSON:
- Go to Firebase Console → Project Settings → Service Accounts
- Click "Generate new private key"
- Save as `secrets/firebase_credentials`

Update `FIREBASE_PROJECT_ID` in `.env`

### 3. Start everything
```bash
./scripts/dev-start.sh
```

Or manually:
```bash
docker-compose up -d
```

### 4. Access the app

| Service       | URL                          | Credentials                        |
|---------------|------------------------------|------------------------------------|
| App           | http://localhost             | Register a new account             |
| API Swagger   | http://localhost:5000/swagger| —                                  |
| Grafana       | http://localhost:3000        | admin / NexChat_Grafana_P@ssw0rd_2024! |
| Kibana        | http://localhost:5601        | elastic / NexChat_Elastic_P@ssw0rd_2024! |
| MinIO         | http://localhost:9001        | nexchat_minio_admin / ...          |
| Kafka UI      | http://localhost:8080        | —                                  |
| MailHog       | http://localhost:8025        | —                                  |
| Prometheus    | http://localhost:9090        | —                                  |

---

## 📁 Project Structure

```
nexchat/
├── backend/                          # .NET 8 solution
│   ├── ChatApp.Domain/               # Entities, Events, Interfaces
│   ├── ChatApp.Application/          # MediatR, DTOs, Validators
│   ├── ChatApp.Infrastructure/       # EF Core, Redis, Kafka, MinIO
│   ├── ChatApp.API/                  # Controllers, SignalR Hub
│   └── ChatApp.Tests/               # Unit + Integration tests
├── frontend/                         # Angular 17 app
│   ├── src/app/
│   │   ├── core/                     # Auth, Services, NgRx
│   │   ├── features/                 # Chat, Calls, Contacts...
│   │   └── shared/                   # Components, Pipes
│   └── electron/                     # Desktop wrapper
├── infrastructure/
│   ├── nginx/                        # Reverse proxy config
│   ├── postgres/                     # DB init + tuning
│   ├── redis/                        # Redis config
│   ├── kafka/                        # Kafka setup
│   ├── minio/                        # Object storage
│   ├── prometheus/                   # Metrics scraping
│   ├── grafana/                      # Dashboards
│   ├── elasticsearch/                # Log storage
│   ├── coturn/                       # WebRTC TURN server
│   └── clamav/                       # Virus scanning
├── scripts/                          # Dev/deploy scripts
├── secrets/                          # Keys & credentials (gitignored)
├── docker-compose.yml                # Development
└── docker-compose.prod.yml           # Production overrides
```

---

## 🔐 Security

- **JWT RS256** asymmetric key signing (4096-bit RSA)
- **Refresh token rotation** with family tracking and reuse detection
- **E2EE** using Signal Protocol (X3DH + Double Ratchet)
- **BCrypt** password hashing (cost factor 12)
- **ClamAV** virus scanning on all file uploads
- **Rate limiting** on all auth endpoints
- **HSTS**, **CSP**, **X-Frame-Options** security headers
- **Pre-signed URLs** for all media (1-hour expiry)

---

## 📊 Kafka Topics

| Topic              | Partitions | Retention | Purpose                    |
|--------------------|-----------|-----------|----------------------------|
| messages.sent      | 6         | 7 days    | New message events         |
| messages.status    | 6         | 7 days    | Delivery/read receipts     |
| notifications.push | 3         | 1 day     | FCM push triggers          |
| calls.events       | 3         | 7 days    | WebRTC call events         |
| presence.update    | 3         | 1 hour    | Online/offline presence    |
| analytics.events   | 6         | 30 days   | Usage analytics            |
| media.processing   | 3         | 1 day     | Image/video processing     |

---

## 🗄️ Redis Key Patterns

| Key Pattern                      | TTL    | Purpose                  |
|----------------------------------|--------|--------------------------|
| `online_users` (SET)             | —      | Active user IDs          |
| `user:{id}:status`               | 30s    | Presence heartbeat       |
| `user:{id}:profile`              | 5m     | Profile cache            |
| `conversation:{id}:messages`     | 1h     | Last 50 messages         |
| `conversation:{id}:typing`       | 3s     | Who is typing            |
| `jwt:blacklist:{jti}`            | = TTL  | Revoked tokens           |
| `otp:{phone}`                    | 5m     | OTP codes                |
| `ratelimit:{ip}:{endpoint}`      | varies | Rate limit counters      |

---

## 🧪 Testing

```bash
# Unit tests
docker-compose exec api dotnet test ChatApp.Tests/ChatApp.Tests.csproj

# With coverage
docker-compose exec api dotnet test --collect:"XPlat Code Coverage"

# Integration tests (requires running infra)
docker-compose exec api dotnet test --filter Category=Integration
```

---

## 📦 Deployment

```bash
# Production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Scale API instances
docker-compose up -d --scale api=3

# View all logs
docker-compose logs -f

# Individual service
docker-compose logs -f api
```

---

## 🔧 Development Commands

```bash
# Apply EF Core migrations
docker-compose exec api dotnet ef database update --project ChatApp.Infrastructure

# Create new migration
docker-compose exec api dotnet ef migrations add MigrationName \
  --project ChatApp.Infrastructure \
  --startup-project ChatApp.API

# Redis CLI
docker-compose exec redis redis-cli -a $REDIS_PASSWORD

# PostgreSQL CLI
docker-compose exec postgres psql -U nexchat_user -d nexchat

# Kafka topics list
docker-compose exec kafka kafka-topics --bootstrap-server localhost:9092 --list
```

---

## 📈 Performance Targets

- **100,000+** concurrent WebSocket connections
- **p99 latency** < 200ms for message delivery
- **SignalR** scales across 3 API replicas via Redis backplane
- **Kafka** handles burst of 50,000+ messages/second
- **Virtual scrolling** in Angular — never renders all messages

---

## 🤝 Contributing

1. Branch from `main`
2. Follow Clean Architecture layer rules
3. Add unit tests for all handlers
4. Run `./scripts/dev-start.sh` to verify
5. PR with description of changes
