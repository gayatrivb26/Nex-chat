# ============================================================
# NexChat Backend - Multi-Stage Dockerfile
# ============================================================

# --- Base runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 5000

# Install curl for healthcheck
RUN apk add --no-cache curl

# Non-root user
RUN addgroup -S nexchat && adduser -S nexchat -G nexchat
USER nexchat

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Restore packages (layer cache optimization)
COPY ChatApp.Domain/ChatApp.Domain.csproj             ChatApp.Domain/
COPY ChatApp.Application/ChatApp.Application.csproj   ChatApp.Application/
COPY ChatApp.Infrastructure/ChatApp.Infrastructure.csproj ChatApp.Infrastructure/
COPY ChatApp.API/ChatApp.API.csproj                   ChatApp.API/
COPY ChatApp.Tests/ChatApp.Tests.csproj               ChatApp.Tests/
COPY NexChat.sln .

RUN dotnet restore NexChat.sln --runtime linux-musl-x64

# Copy source
COPY . .

# Build
RUN dotnet build NexChat.sln \
    --configuration Release \
    --runtime linux-musl-x64 \
    --no-restore \
    -p:UseAppHost=false

# --- Test stage ---
FROM build AS test
RUN dotnet test ChatApp.Tests/ChatApp.Tests.csproj \
    --configuration Release \
    --no-build \
    --logger "trx;LogFileName=results.trx" \
    --results-directory /TestResults

# --- Publish stage ---
FROM build AS publish
RUN dotnet publish ChatApp.API/ChatApp.API.csproj \
    --configuration Release \
    --runtime linux-musl-x64 \
    --self-contained false \
    --no-restore \
    -p:UseAppHost=false \
    -o /app/publish

# --- Development stage (hot reload) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS development
WORKDIR /app

RUN apk add --no-cache curl

# Install dotnet-ef tool
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

COPY --from=build /src .

ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

HEALTHCHECK --interval=15s --timeout=5s --retries=10 --start-period=60s \
    CMD curl -f http://localhost:5000/health/live || exit 1

CMD ["dotnet", "watch", "run", "--project", "ChatApp.API/ChatApp.API.csproj", \
     "--urls", "http://+:5000"]

# --- Production stage ---
FROM base AS production
WORKDIR /app

COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=15s --timeout=5s --retries=10 --start-period=60s \
    CMD curl -f http://localhost:5000/health/live || exit 1

ENTRYPOINT ["dotnet", "ChatApp.API.dll"]
