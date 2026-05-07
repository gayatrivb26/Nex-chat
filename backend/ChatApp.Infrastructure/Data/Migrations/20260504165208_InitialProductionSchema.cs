using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace ChatApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialProductionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_secret = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    backup_codes = table.Column<string>(type: "jsonb", nullable: true),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    identity_public_key = table.Column<string>(type: "text", nullable: true),
                    signed_pre_key_id = table.Column<string>(type: "text", nullable: true),
                    signed_pre_key = table.Column<string>(type: "text", nullable: true),
                    signed_pre_key_signature = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "KeyBundles",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_key = table.Column<string>(type: "text", nullable: false),
                    signed_pre_key_id = table.Column<int>(type: "integer", nullable: false),
                    signed_pre_key = table.Column<string>(type: "text", nullable: false),
                    signed_pre_key_sig = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_key_bundles", x => x.id);
                    table.ForeignKey(
                        name: "fk_key_bundles_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaFiles",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    original_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    stored_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    bucket_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    duration = table.Column<int>(type: "integer", nullable: true),
                    thumbnail_path = table.Column<string>(type: "text", nullable: true),
                    checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_scanned = table.Column<bool>(type: "boolean", nullable: false),
                    scan_result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_media_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_media_files_user_uploaded_by_id",
                        column: x => x.uploaded_by_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    body = table.Column<string>(type: "text", nullable: true),
                    payload = table.Column<string>(type: "jsonb", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OneTimePreKeys",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_id = table.Column<int>(type: "integer", nullable: false),
                    public_key = table.Column<string>(type: "text", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_one_time_pre_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_one_time_pre_keys_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    device_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserContacts",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_blocked = table.Column<bool>(type: "boolean", nullable: false),
                    blocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_contacts_users_contact_user_id",
                        column: x => x.contact_user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_contacts_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CallLogs",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    initiator_id = table.Column<Guid>(type: "uuid", nullable: true),
                    call_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false),
                    end_reason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    recording_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_call_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_call_logs_user_initiator_id",
                        column: x => x.initiator_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConversationMembers",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_muted = table.Column<bool>(type: "boolean", nullable: false),
                    mute_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_read_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notifications_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversation_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversation_members_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversations", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversations_user_created_by_id",
                        column: x => x.created_by_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    encrypted_content = table.Column<byte[]>(type: "bytea", nullable: true),
                    message_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    media_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    media_duration = table.Column<int>(type: "integer", nullable: true),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    reply_to_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    forwarded_from_message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false),
                    edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delete_for_everyone = table.Column<bool>(type: "boolean", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalSchema: "public",
                        principalTable: "Conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_messages_messages_reply_to_message_id",
                        column: x => x.reply_to_message_id,
                        principalSchema: "public",
                        principalTable: "Messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_messages_user_sender_id",
                        column: x => x.sender_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MessageReactions",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    emoji = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_reactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_reactions_messages_message_id",
                        column: x => x.message_id,
                        principalSchema: "public",
                        principalTable: "Messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_message_reactions_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageStatuses",
                schema: "public",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_statuses", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_statuses_messages_message_id",
                        column: x => x.message_id,
                        principalSchema: "public",
                        principalTable: "Messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_message_statuses_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id_created_at",
                schema: "public",
                table: "AuditLogs",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_call_logs_conversation_id_started_at",
                schema: "public",
                table: "CallLogs",
                columns: new[] { "conversation_id", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_call_logs_initiator_id",
                schema: "public",
                table: "CallLogs",
                column: "initiator_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversation_members_conversation_id_user_id",
                schema: "public",
                table: "ConversationMembers",
                columns: new[] { "conversation_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversation_members_user_id",
                schema: "public",
                table: "ConversationMembers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_conversations_activity",
                schema: "public",
                table: "Conversations",
                column: "last_activity_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_conversations_created_by_id",
                schema: "public",
                table: "Conversations",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversations_last_message_id",
                schema: "public",
                table: "Conversations",
                column: "last_message_id");

            migrationBuilder.CreateIndex(
                name: "ix_key_bundles_user_id",
                schema: "public",
                table: "KeyBundles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_media_files_stored_name",
                schema: "public",
                table: "MediaFiles",
                column: "stored_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_media_files_uploaded_by_id",
                schema: "public",
                table: "MediaFiles",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_reactions_message_id_user_id_emoji",
                schema: "public",
                table: "MessageReactions",
                columns: new[] { "message_id", "user_id", "emoji" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_reactions_user_id",
                schema: "public",
                table: "MessageReactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_messages_conversation_created",
                schema: "public",
                table: "Messages",
                columns: new[] { "conversation_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_messages_search",
                schema: "public",
                table: "Messages",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "idx_messages_sender",
                schema: "public",
                table: "Messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_reply_to_message_id",
                schema: "public",
                table: "Messages",
                column: "reply_to_message_id");

            migrationBuilder.CreateIndex(
                name: "idx_messagestatus_user",
                schema: "public",
                table: "MessageStatuses",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_message_statuses_message_id_user_id",
                schema: "public",
                table: "MessageStatuses",
                columns: new[] { "message_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notifications_user_unread",
                schema: "public",
                table: "Notifications",
                columns: new[] { "user_id", "is_read", "created_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_one_time_pre_keys_user_id_is_used",
                schema: "public",
                table: "OneTimePreKeys",
                columns: new[] { "user_id", "is_used" });

            migrationBuilder.CreateIndex(
                name: "ix_one_time_pre_keys_user_id_key_id",
                schema: "public",
                table: "OneTimePreKeys",
                columns: new[] { "user_id", "key_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_refreshtokens_family",
                schema: "public",
                table: "RefreshTokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                schema: "public",
                table: "RefreshTokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id",
                schema: "public",
                table: "RefreshTokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_contacts_contact_user_id",
                schema: "public",
                table: "UserContacts",
                column: "contact_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_contacts_user_id_contact_user_id",
                schema: "public",
                table: "UserContacts",
                columns: new[] { "user_id", "contact_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "public",
                table: "Users",
                column: "email",
                unique: true,
                filter: "email IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_users_phone",
                schema: "public",
                table: "Users",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_status",
                schema: "public",
                table: "Users",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_users_username",
                schema: "public",
                table: "Users",
                column: "username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_call_logs_conversation_conversation_id",
                schema: "public",
                table: "CallLogs",
                column: "conversation_id",
                principalSchema: "public",
                principalTable: "Conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_conversation_members_conversations_conversation_id",
                schema: "public",
                table: "ConversationMembers",
                column: "conversation_id",
                principalSchema: "public",
                principalTable: "Conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_conversations_message_last_message_id",
                schema: "public",
                table: "Conversations",
                column: "last_message_id",
                principalSchema: "public",
                principalTable: "Messages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION update_messages_search_vector()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.search_vector := to_tsvector('pg_catalog.english', COALESCE(NEW.content, ''));
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER messages_search_update
                BEFORE INSERT OR UPDATE ON public."Messages"
                FOR EACH ROW EXECUTE FUNCTION update_messages_search_vector();
                """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION update_updated_at()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.updated_at = NOW();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER users_updated_at
                BEFORE UPDATE ON public."Users"
                FOR EACH ROW EXECUTE FUNCTION update_updated_at();
                """);

            migrationBuilder.Sql("""
                INSERT INTO public."Users" (
                    id, username, phone, password_hash, display_name, is_verified, status, created_at, updated_at
                ) VALUES (
                    '00000000-0000-0000-0000-000000000001',
                    'system',
                    '+00000000000',
                    '$2a$12$system_placeholder_not_usable_for_login',
                    'ChatApp System',
                    TRUE,
                    'Offline',
                    NOW(),
                    NOW()
                ) ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS users_updated_at ON public.\"Users\";");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_updated_at();");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS messages_search_update ON public.\"Messages\";");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_messages_search_vector();");

            migrationBuilder.DropForeignKey(
                name: "fk_conversations_user_created_by_id",
                schema: "public",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_user_sender_id",
                schema: "public",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "fk_messages_conversations_conversation_id",
                schema: "public",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CallLogs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ConversationMembers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "KeyBundles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MediaFiles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MessageReactions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "MessageStatuses",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OneTimePreKeys",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "public");

            migrationBuilder.DropTable(
                name: "UserContacts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Conversations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "public");
        }
    }
}
