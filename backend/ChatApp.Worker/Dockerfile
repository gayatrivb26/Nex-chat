# Worker Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

COPY ChatApp.Domain/ChatApp.Domain.csproj ChatApp.Domain/
COPY ChatApp.Application/ChatApp.Application.csproj ChatApp.Application/
COPY ChatApp.Infrastructure/ChatApp.Infrastructure.csproj ChatApp.Infrastructure/
COPY ChatApp.Worker/ChatApp.Worker.csproj ChatApp.Worker/
COPY NexChat.sln .

RUN dotnet restore ChatApp.Worker/ChatApp.Worker.csproj --runtime linux-musl-x64

COPY . .

RUN dotnet publish ChatApp.Worker/ChatApp.Worker.csproj \
    --configuration Release \
    --runtime linux-musl-x64 \
    --self-contained false \
    -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ChatApp.Worker.dll"]
