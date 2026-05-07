-- ============================================================
-- NexChat - PostgreSQL Initialization & Full Schema
-- ============================================================
-- This runs once on first container startup.
-- EF Core migrations handle subsequent schema changes.
-- ============================================================

-- Extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Custom types (ENUMs)
DO $$ BEGIN
    CREATE TYPE user_status AS ENUM ('online', 'offline', 'away', 'busy');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE conversation_type AS ENUM ('private', 'group', 'broadcast');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE member_role AS ENUM ('owner', 'admin', 'member');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE message_type AS ENUM (
        'text', 'image', 'video', 'audio', 'file',
        'voice', 'location', 'contact', 'sticker', 'system'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE message_status_type AS ENUM ('sent', 'delivered', 'read');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE call_type AS ENUM ('audio', 'video');
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

DO $$ BEGIN
    CREATE TYPE call_status AS ENUM (
        'initiated', 'ringing', 'answered', 'rejected',
        'missed', 'ended', 'failed'
    );
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- ============================================================
-- USERS
-- ============================================================
CREATE TABLE IF NOT EXISTS "Users" (
    "Id"                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Username"              VARCHAR(30)  UNIQUE NOT NULL,
    "Email"                 VARCHAR(255) UNIQUE,
    "Phone"                 VARCHAR(20)  UNIQUE NOT NULL,
    "PasswordHash"          VARCHAR(255) NOT NULL,
    "AvatarUrl"             TEXT,
    "DisplayName"           VARCHAR(100),
    "Bio"                   VARCHAR(500),
    "Status"                user_status  DEFAULT 'offline',
    "LastSeen"              TIMESTAMPTZ,
    "IsVerified"            BOOLEAN      DEFAULT FALSE,
    "IsDeleted"             BOOLEAN      DEFAULT FALSE,
    "DeletedAt"             TIMESTAMPTZ,
    "TwoFactorEnabled"      BOOLEAN      DEFAULT FALSE,
    "TwoFactorSecret"       VARCHAR(255),
    "BackupCodes"           JSONB,
    "FailedLoginAttempts"   INTEGER      DEFAULT 0,
    "LockedUntil"           TIMESTAMPTZ,
    "PublicKey"             TEXT,        -- E2EE: Signal Protocol identity key (base64)
    "SignedPreKey"          JSONB,       -- E2EE: signed pre-key bundle
    "OneTimePreKeys"        JSONB,       -- E2EE: one-time pre-key pool
    "CreatedAt"             TIMESTAMPTZ  DEFAULT NOW(),
    "UpdatedAt"             TIMESTAMPTZ  DEFAULT NOW()
);

-- ============================================================
-- CONVERSATIONS
-- ============================================================
CREATE TABLE IF NOT EXISTS "Conversations" (
    "Id"                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Type"              conversation_type NOT NULL,
    "Name"              VARCHAR(100),
    "Description"       VARCHAR(500),
    "AvatarUrl"         TEXT,
    "CreatedById"       UUID REFERENCES "Users"("Id") ON DELETE SET NULL,
    "LastMessageId"     UUID,
    "LastActivityAt"    TIMESTAMPTZ DEFAULT NOW(),
    "IsDeleted"         BOOLEAN     DEFAULT FALSE,
    "CreatedAt"         TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================================
-- CONVERSATION MEMBERS
-- ============================================================
CREATE TABLE IF NOT EXISTS "ConversationMembers" (
    "Id"                        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ConversationId"            UUID REFERENCES "Conversations"("Id") ON DELETE CASCADE,
    "UserId"                    UUID REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Role"                      member_role DEFAULT 'member',
    "JoinedAt"                  TIMESTAMPTZ DEFAULT NOW(),
    "LeftAt"                    TIMESTAMPTZ,
    "IsMuted"                   BOOLEAN     DEFAULT FALSE,
    "MuteUntil"                 TIMESTAMPTZ,
    "LastReadMessageId"         UUID,
    "LastReadAt"                TIMESTAMPTZ,
    "NotificationsEnabled"      BOOLEAN     DEFAULT TRUE,
    UNIQUE("ConversationId", "UserId")
);

-- ============================================================
-- MESSAGES
-- ============================================================
CREATE TABLE IF NOT EXISTS "Messages" (
    "Id"                        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ConversationId"            UUID REFERENCES "Conversations"("Id") ON DELETE CASCADE,
    "SenderId"                  UUID REFERENCES "Users"("Id") ON DELETE SET NULL,
    "Content"                   TEXT,
    "EncryptedContent"          BYTEA,       -- E2EE: encrypted payload
    "MessageType"               message_type NOT NULL,
    "MediaUrl"                  TEXT,
    "ThumbnailUrl"              TEXT,
    "FileName"                  VARCHAR(255),
    "FileSize"                  BIGINT,
    "MediaDuration"             INTEGER,
    "MimeType"                  VARCHAR(100),
    "ReplyToMessageId"          UUID REFERENCES "Messages"("Id") ON DELETE SET NULL,
    "ForwardedFromMessageId"    UUID REFERENCES "Messages"("Id") ON DELETE SET NULL,
    "IsEdited"                  BOOLEAN     DEFAULT FALSE,
    "EditedAt"                  TIMESTAMPTZ,
    "IsDeleted"                 BOOLEAN     DEFAULT FALSE,
    "DeletedAt"                 TIMESTAMPTZ,
    "DeleteForEveryone"         BOOLEAN     DEFAULT FALSE,
    "Metadata"                  JSONB,
    "SearchVector"              TSVECTOR,
    "CreatedAt"                 TIMESTAMPTZ DEFAULT NOW()
);

-- Self-referencing FK after table creation
ALTER TABLE "Conversations"
    ADD CONSTRAINT IF NOT EXISTS "FK_Conversations_LastMessage"
    FOREIGN KEY ("LastMessageId") REFERENCES "Messages"("Id") ON DELETE SET NULL;

-- ============================================================
-- MESSAGE STATUS (per recipient)
-- ============================================================
CREATE TABLE IF NOT EXISTS "MessageStatuses" (
    "Id"            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MessageId"     UUID REFERENCES "Messages"("Id") ON DELETE CASCADE,
    "UserId"        UUID REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Status"        message_status_type DEFAULT 'sent',
    "DeliveredAt"   TIMESTAMPTZ,
    "ReadAt"        TIMESTAMPTZ,
    UNIQUE("MessageId", "UserId")
);

-- ============================================================
-- MESSAGE REACTIONS
-- ============================================================
CREATE TABLE IF NOT EXISTS "MessageReactions" (
    "Id"            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "MessageId"     UUID REFERENCES "Messages"("Id") ON DELETE CASCADE,
    "UserId"        UUID REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Emoji"         VARCHAR(10) NOT NULL,
    "CreatedAt"     TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE("MessageId", "UserId", "Emoji")
);

-- ============================================================
-- USER CONTACTS
-- ============================================================
CREATE TABLE IF NOT EXISTS "UserContacts" (
    "Id"                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"            UUID REFERENCES "Users"("Id") ON DELETE CASCADE,
    "ContactUserId"     UUID REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Nickname"          VARCHAR(100),
    "IsBlocked"         BOOLEAN     DEFAULT FALSE,
    "BlockedAt"         TIMESTAMPTZ,
    "CreatedAt"         TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE("UserId", "ContactUserId")
);

-- ============================================================
-- CALL LOGS
-- ============================================================
CREATE TABLE IF NOT EXISTS "CallLogs" (
    "Id"                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ConversationId"    UUID REFERENCES "Conversations"("Id") ON DELETE SET NULL,
    "InitiatorId"       UUID REFERENCES "Users"("Id") ON DELETE SET NULL,
    "CallType"          call_type    NOT NULL,
    "Status"            call_status  NOT NULL,
    "StartedAt"         TIMESTAMPTZ  DEFAULT NOW(),
    "AnsweredAt"        TIMESTAMPTZ,
    "EndedAt"           TIMESTAMPTZ,
    "DurationSeconds"   INTEGER      DEFAULT 0,
    "EndReason"         VARCHAR(50),
    "RecordingUrl"      TEXT
);

-- ============================================================
-- REFRESH TOKENS
-- ============================================================
CREATE TABLE IF NOT EXISTS "RefreshTokens" (
    "Id"                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"            UUID REFERENCES "Users"("Id") ON DELETE CASCADE,
    "TokenHash"         VARCHAR(255) UNIQUE NOT NULL,
    "FamilyId"          UUID         NOT NULL,
    "DeviceName"        VARCHAR(255),
    "DeviceType"        VARCHAR(50),
    "IpAddress"         VARCHAR(45),
    "UserAgent"         TEXT,
    "ExpiresAt"         TIMESTAMPTZ  NOT NULL,
    "RevokedAt"         TIMESTAMPTZ,
    "ReplacedByToken"   UUID,
    "CreatedAt"         TIMESTAMPTZ  DEFAULT NOW()
);

-- ============================================================
-- NOTIFICATIONS
-- ============================================================
CREATE TABLE IF NOT EXISTS "Notifications" (
    "Id"            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"        UUID REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Type"          VARCHAR(50)  NOT NULL,
    "Title"         VARCHAR(255),
    "Body"          TEXT,
    "Payload"       JSONB,
    "ImageUrl"      TEXT,
    "IsRead"        BOOLEAN      DEFAULT FALSE,
    "ReadAt"        TIMESTAMPTZ,
    "CreatedAt"     TIMESTAMPTZ  DEFAULT NOW()
);

-- ============================================================
-- AUDIT LOGS
-- ============================================================
CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id"            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"        UUID REFERENCES "Users"("Id") ON DELETE SET NULL,
    "Action"        VARCHAR(100) NOT NULL,
    "EntityType"    VARCHAR(100),
    "EntityId"      UUID,
    "OldValues"     JSONB,
    "NewValues"     JSONB,
    "IpAddress"     VARCHAR(45),
    "UserAgent"     TEXT,
    "CreatedAt"     TIMESTAMPTZ  DEFAULT NOW()
);

-- ============================================================
-- MEDIA FILES
-- ============================================================
CREATE TABLE IF NOT EXISTS "MediaFiles" (
    "Id"            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UploadedById"  UUID REFERENCES "Users"("Id") ON DELETE SET NULL,
    "OriginalName"  VARCHAR(255),
    "StoredName"    VARCHAR(255) UNIQUE NOT NULL,
    "BucketName"    VARCHAR(100) NOT NULL,
    "FilePath"      TEXT         NOT NULL,
    "MimeType"      VARCHAR(100),
    "FileSize"      BIGINT,
    "Width"         INTEGER,
    "Height"        INTEGER,
    "Duration"      INTEGER,
    "ThumbnailPath" TEXT,
    "Checksum"      VARCHAR(64),
    "IsScanned"     BOOLEAN      DEFAULT FALSE,
    "ScanResult"    VARCHAR(50),
    "CreatedAt"     TIMESTAMPTZ  DEFAULT NOW()
);

-- ============================================================
-- E2EE KEY BUNDLES (Signal Protocol)
-- ============================================================
CREATE TABLE IF NOT EXISTS "KeyBundles" (
    "Id"                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"            UUID UNIQUE REFERENCES "Users"("Id") ON DELETE CASCADE,
    "IdentityKey"       TEXT NOT NULL,       -- Curve25519 identity key (base64)
    "SignedPreKeyId"    INTEGER NOT NULL,
    "SignedPreKey"      TEXT NOT NULL,       -- base64
    "SignedPreKeySig"   TEXT NOT NULL,       -- base64
    "UpdatedAt"         TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS "OneTimePreKeys" (
    "Id"            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"        UUID REFERENCES "Users"("Id") ON DELETE CASCADE,
    "KeyId"         INTEGER NOT NULL,
    "PublicKey"     TEXT NOT NULL,           -- base64
    "IsUsed"        BOOLEAN DEFAULT FALSE,
    "UsedAt"        TIMESTAMPTZ,
    "CreatedAt"     TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE("UserId", "KeyId")
);

-- ============================================================
-- INDEXES
-- ============================================================

-- Users
CREATE INDEX IF NOT EXISTS "IX_Users_Phone"       ON "Users"("Phone");
CREATE INDEX IF NOT EXISTS "IX_Users_Email"        ON "Users"("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_Username"     ON "Users"("Username");
CREATE INDEX IF NOT EXISTS "IX_Users_Status"       ON "Users"("Status") WHERE "IsDeleted" = FALSE;

-- Messages (critical - heavy query path)
CREATE INDEX IF NOT EXISTS "IX_Messages_Conversation_Created"
    ON "Messages"("ConversationId", "CreatedAt" DESC)
    WHERE "IsDeleted" = FALSE;

CREATE INDEX IF NOT EXISTS "IX_Messages_Sender"
    ON "Messages"("SenderId");

CREATE INDEX IF NOT EXISTS "IX_Messages_Search"
    ON "Messages" USING GIN("SearchVector");

CREATE INDEX IF NOT EXISTS "IX_Messages_ReplyTo"
    ON "Messages"("ReplyToMessageId")
    WHERE "ReplyToMessageId" IS NOT NULL;

-- Message Status
CREATE INDEX IF NOT EXISTS "IX_MessageStatuses_User_Status"
    ON "MessageStatuses"("UserId", "Status");

CREATE INDEX IF NOT EXISTS "IX_MessageStatuses_Message"
    ON "MessageStatuses"("MessageId");

-- Conversations
CREATE INDEX IF NOT EXISTS "IX_Conversations_LastActivity"
    ON "Conversations"("LastActivityAt" DESC)
    WHERE "IsDeleted" = FALSE;

-- Conversation Members
CREATE INDEX IF NOT EXISTS "IX_ConversationMembers_User"
    ON "ConversationMembers"("UserId")
    WHERE "LeftAt" IS NULL;

CREATE INDEX IF NOT EXISTS "IX_ConversationMembers_Conversation"
    ON "ConversationMembers"("ConversationId")
    WHERE "LeftAt" IS NULL;

-- Notifications
CREATE INDEX IF NOT EXISTS "IX_Notifications_User_Unread"
    ON "Notifications"("UserId", "IsRead", "CreatedAt" DESC);

-- Refresh Tokens
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_Family"
    ON "RefreshTokens"("FamilyId");

CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_User"
    ON "RefreshTokens"("UserId")
    WHERE "RevokedAt" IS NULL;

-- Audit Logs
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_User_Created"
    ON "AuditLogs"("UserId", "CreatedAt" DESC);

-- Call Logs
CREATE INDEX IF NOT EXISTS "IX_CallLogs_Conversation"
    ON "CallLogs"("ConversationId", "StartedAt" DESC);

-- One-time pre-keys
CREATE INDEX IF NOT EXISTS "IX_OneTimePreKeys_User_Unused"
    ON "OneTimePreKeys"("UserId")
    WHERE "IsUsed" = FALSE;

-- ============================================================
-- TRIGGERS
-- ============================================================

-- Auto-update search_vector on Messages
CREATE OR REPLACE FUNCTION update_messages_search_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW."SearchVector" := to_tsvector('pg_catalog.english', COALESCE(NEW."Content", ''));
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Messages_SearchVector" ON "Messages";
CREATE TRIGGER "TR_Messages_SearchVector"
    BEFORE INSERT OR UPDATE ON "Messages"
    FOR EACH ROW EXECUTE FUNCTION update_messages_search_vector();

-- Auto-update Users.UpdatedAt
CREATE OR REPLACE FUNCTION update_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW."UpdatedAt" = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Users_UpdatedAt" ON "Users";
CREATE TRIGGER "TR_Users_UpdatedAt"
    BEFORE UPDATE ON "Users"
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

-- Auto-update Conversations.LastActivityAt when a message is inserted
CREATE OR REPLACE FUNCTION update_conversation_last_activity()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE "Conversations"
    SET "LastActivityAt" = NOW(),
        "LastMessageId" = NEW."Id"
    WHERE "Id" = NEW."ConversationId";
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS "TR_Messages_ConversationActivity" ON "Messages";
CREATE TRIGGER "TR_Messages_ConversationActivity"
    AFTER INSERT ON "Messages"
    FOR EACH ROW EXECUTE FUNCTION update_conversation_last_activity();

-- ============================================================
-- INITIAL DATA
-- ============================================================

-- System user for system messages
INSERT INTO "Users" (
    "Id", "Username", "Phone", "PasswordHash", "DisplayName", "IsVerified", "Status"
) VALUES (
    '00000000-0000-0000-0000-000000000001',
    'system',
    '+00000000000',
    '$2a$12$system_placeholder_not_usable_for_login',
    'NexChat System',
    TRUE,
    'offline'
) ON CONFLICT DO NOTHING;

-- ============================================================
RAISE NOTICE 'NexChat database initialized successfully';
