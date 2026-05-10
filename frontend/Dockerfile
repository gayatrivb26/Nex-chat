# ============================================================
# NexChat Frontend - Multi-Stage Dockerfile
# ============================================================

# --- Development stage ---
FROM node:20-alpine AS development
WORKDIR /app

# Install dependencies first (layer cache)
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

EXPOSE 4200

HEALTHCHECK --interval=20s --timeout=5s --retries=10 --start-period=90s \
    CMD wget --quiet --tries=1 --spider http://localhost:4200 || exit 1

CMD ["npm", "run", "start", "--", \
     "--host", "0.0.0.0", \
     "--port", "4200", \
     "--disable-host-check", \
     "--poll", "2000"]

# --- Build stage ---
FROM node:20-alpine AS build
WORKDIR /app

ARG API_URL=http://localhost/api
ARG WS_URL=ws://localhost/hubs
ENV NODE_ENV=production

COPY package.json package-lock.json ./
RUN npm ci --omit=dev

COPY . .

RUN npm run build -- \
    --configuration production \
    --output-path /app/dist/nexchat

# --- Production stage (Nginx) ---
FROM nginx:alpine AS production
WORKDIR /usr/share/nginx/html

# Remove default nginx content
RUN rm -rf ./*

COPY --from=build /app/dist/nexchat .
COPY nginx-frontend.conf /etc/nginx/conf.d/default.conf

# Copy entrypoint script that will write runtime config from env vars
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

EXPOSE 80

HEALTHCHECK --interval=15s --timeout=5s --retries=5 \
    CMD wget --quiet --tries=1 --spider http://localhost:80 || exit 1

ENTRYPOINT ["/docker-entrypoint.sh"]
