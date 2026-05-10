#!/bin/sh
set -e

# Generate runtime config for frontend from environment variables
API_BASE=${API_BASE:-/api/v1}
WS_BASE=${WS_BASE:-/hubs}

mkdir -p /usr/share/nginx/html/assets
cat > /usr/share/nginx/html/assets/runtime-config.json <<-JSON
{
  "apiBase": "${API_BASE}",
  "wsBase": "${WS_BASE}"
}
JSON

# Start nginx
exec nginx -g 'daemon off;'
