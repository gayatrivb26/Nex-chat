#!/bin/bash
# ============================================================
# NexChat - Generate Secrets & Keys
# ============================================================
# Run this ONCE before first docker-compose up
# Usage: ./scripts/generate-secrets.sh
# ============================================================

set -euo pipefail

SECRETS_DIR="./secrets"
mkdir -p "$SECRETS_DIR"

echo ""
echo "╔══════════════════════════════════════════════╗"
echo "║      NexChat - Generating Secrets            ║"
echo "╚══════════════════════════════════════════════╝"
echo ""

# --- JWT RS256 Key Pair ---
echo "[1/6] Generating JWT RS256 key pair..."
if [ ! -f "$SECRETS_DIR/jwt_private_key" ]; then
    openssl genrsa -out "$SECRETS_DIR/jwt_private_key" 4096 2>/dev/null
    openssl rsa -in "$SECRETS_DIR/jwt_private_key" \
        -pubout -out "$SECRETS_DIR/jwt_public_key" 2>/dev/null
    chmod 600 "$SECRETS_DIR/jwt_private_key"
    chmod 644 "$SECRETS_DIR/jwt_public_key"
    echo "  ✓ JWT RS256 keys generated"
else
    echo "  ℹ  JWT keys already exist, skipping"
fi

# --- Firebase Credentials Placeholder ---
echo "[2/6] Setting up Firebase credentials placeholder..."
if [ ! -f "$SECRETS_DIR/firebase_credentials" ]; then
    cat > "$SECRETS_DIR/firebase_credentials" << 'EOF'
{
  "type": "service_account",
  "project_id": "YOUR_FIREBASE_PROJECT_ID",
  "private_key_id": "YOUR_PRIVATE_KEY_ID",
  "private_key": "-----BEGIN RSA PRIVATE KEY-----\nYOUR_PRIVATE_KEY\n-----END RSA PRIVATE KEY-----\n",
  "client_email": "firebase-adminsdk@YOUR_PROJECT.iam.gserviceaccount.com",
  "client_id": "YOUR_CLIENT_ID",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token"
}
EOF
    echo "  ✓ Firebase credentials placeholder created"
    echo "  ⚠  IMPORTANT: Replace secrets/firebase_credentials with your real Firebase service account JSON!"
else
    echo "  ℹ  Firebase credentials already exist, skipping"
fi

# --- DB Password ---
echo "[3/6] Generating database password..."
if [ ! -f "$SECRETS_DIR/postgres_password" ]; then
    openssl rand -base64 32 > "$SECRETS_DIR/postgres_password"
    chmod 600 "$SECRETS_DIR/postgres_password"
    echo "  ✓ PostgreSQL password generated"
fi

# --- Redis Password ---
echo "[4/6] Generating Redis password..."
if [ ! -f "$SECRETS_DIR/redis_password" ]; then
    openssl rand -base64 32 > "$SECRETS_DIR/redis_password"
    chmod 600 "$SECRETS_DIR/redis_password"
    echo "  ✓ Redis password generated"
fi

# --- Encryption Key (for field-level encryption) ---
echo "[5/6] Generating field-level encryption key..."
if [ ! -f "$SECRETS_DIR/encryption_key" ]; then
    openssl rand -hex 32 > "$SECRETS_DIR/encryption_key"
    chmod 600 "$SECRETS_DIR/encryption_key"
    echo "  ✓ Encryption key generated (AES-256)"
fi

# --- .gitignore for secrets ---
echo "[6/6] Creating secrets .gitignore..."
cat > "$SECRETS_DIR/.gitignore" << 'EOF'
# NEVER commit secrets to git!
*
!.gitignore
!README.md
EOF

cat > "$SECRETS_DIR/README.md" << 'EOF'
# Secrets Directory

This directory contains sensitive credentials. NEVER commit these files to git.

## Files
- `jwt_private_key`      - RSA 4096-bit private key for JWT signing (RS256)
- `jwt_public_key`       - RSA public key for JWT verification
- `firebase_credentials` - Firebase Admin SDK service account JSON
- `postgres_password`    - PostgreSQL database password
- `redis_password`       - Redis password
- `encryption_key`       - AES-256 key for field-level encryption

## Setup
1. Run `./scripts/generate-secrets.sh` to generate keys
2. Replace `firebase_credentials` with your real Firebase service account JSON
3. Update `.env` if you want to override generated passwords
EOF

echo ""
echo "╔══════════════════════════════════════════════╗"
echo "║           Secrets Generated!                 ║"
echo "╚══════════════════════════════════════════════╝"
echo ""
echo "Next steps:"
echo "  1. Replace secrets/firebase_credentials with real Firebase JSON"
echo "  2. Review and update .env file"
echo "  3. Run: docker-compose up -d"
echo ""
echo "⚠  NEVER commit the secrets/ directory to git!"
echo ""
