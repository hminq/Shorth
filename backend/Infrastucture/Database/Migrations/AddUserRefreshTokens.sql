CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE TYPE identity_provider AS ENUM ('local', 'google', 'github');
    CREATE TYPE otp_purpose AS ENUM ('email_verification', 'password_reset');
    CREATE TYPE user_status AS ENUM ('pending_verification', 'active', 'disabled');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        email character varying(320),
        email_normalized character varying(320),
        display_name character varying(100),
        avatar_url character varying(500),
        email_verified_at timestamp with time zone,
        status user_status NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        last_login_at timestamp with time zone,
        CONSTRAINT "PK_users" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE TABLE links (
        id uuid NOT NULL,
        owner_id uuid,
        slug character varying(6) NOT NULL,
        destination_url text NOT NULL,
        click_count bigint NOT NULL,
        last_clicked_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        expires_at timestamp with time zone,
        is_disabled boolean NOT NULL,
        CONSTRAINT "PK_links" PRIMARY KEY (id),
        CONSTRAINT "FK_links_users_owner_id" FOREIGN KEY (owner_id) REFERENCES users (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE TABLE user_identities (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        provider identity_provider NOT NULL,
        provider_user_id character varying(320) NOT NULL,
        provider_email character varying(320),
        password_hash character varying(255),
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_identities" PRIMARY KEY (id),
        CONSTRAINT "FK_user_identities_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE TABLE user_otps (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        purpose otp_purpose NOT NULL,
        code_hash character varying(255) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        used_at timestamp with time zone,
        invalidated_at timestamp with time zone,
        attempt_count integer NOT NULL,
        max_attempts integer NOT NULL,
        last_sent_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_otps" PRIMARY KEY (id),
        CONSTRAINT "FK_user_otps_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE TABLE link_click_events (
        id uuid NOT NULL,
        link_id uuid NOT NULL,
        clicked_at timestamp with time zone NOT NULL,
        user_agent text,
        referrer text,
        ip_hash character varying(128),
        country_code character varying(2),
        device_type character varying(50),
        browser_family character varying(100),
        os_family character varying(100),
        CONSTRAINT "PK_link_click_events" PRIMARY KEY (id),
        CONSTRAINT "FK_link_click_events_links_link_id" FOREIGN KEY (link_id) REFERENCES links (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE TABLE link_daily_stats (
        link_id uuid NOT NULL,
        date date NOT NULL,
        clicks integer NOT NULL,
        unique_visitors integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_link_daily_stats" PRIMARY KEY (link_id, date),
        CONSTRAINT "FK_link_daily_stats_links_link_id" FOREIGN KEY (link_id) REFERENCES links (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE INDEX "IX_link_click_events_link_id" ON link_click_events (link_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE INDEX "IX_link_click_events_link_id_clicked_at" ON link_click_events (link_id, clicked_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE INDEX "IX_links_owner_id" ON links (owner_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_links_slug" ON links (slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_user_identities_provider_provider_user_id" ON user_identities (provider, provider_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE INDEX "IX_user_identities_user_id" ON user_identities (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE INDEX "IX_user_otps_expires_at" ON user_otps (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE INDEX "IX_user_otps_user_id_purpose" ON user_otps (user_id, purpose);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_users_email_normalized" ON users (email_normalized) WHERE "email_normalized" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260508074659_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260508074659_InitialCreate', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517091733_AddOutboxMessages') THEN
    CREATE TYPE outbox_message_status AS ENUM ('pending', 'processing', 'processed', 'failed');
    CREATE TYPE outbox_message_type AS ENUM ('email_job');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517091733_AddOutboxMessages') THEN
    CREATE TABLE outbox_messages (
        id uuid NOT NULL,
        type outbox_message_type NOT NULL,
        payload jsonb NOT NULL,
        status outbox_message_status NOT NULL,
        retry_count integer NOT NULL,
        next_attempt_at timestamp with time zone NOT NULL,
        locked_until timestamp with time zone,
        processed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_outbox_messages" PRIMARY KEY (id),
        CONSTRAINT ck_outbox_messages_retry_count_range CHECK (retry_count >= 0 AND retry_count <= 10)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517091733_AddOutboxMessages') THEN
    CREATE INDEX ix_outbox_messages_status_next_attempt_at ON outbox_messages (status, next_attempt_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517091733_AddOutboxMessages') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260517091733_AddOutboxMessages', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260521091145_AddUserRefreshTokens') THEN
    CREATE TABLE user_refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token_hash character varying(255) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        replaced_by_token_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_user_refresh_tokens" PRIMARY KEY (id),
        CONSTRAINT "FK_user_refresh_tokens_user_refresh_tokens_replaced_by_token_id" FOREIGN KEY (replaced_by_token_id) REFERENCES user_refresh_tokens (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_user_refresh_tokens_users_user_id" FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260521091145_AddUserRefreshTokens') THEN
    CREATE INDEX "IX_user_refresh_tokens_expires_at" ON user_refresh_tokens (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260521091145_AddUserRefreshTokens') THEN
    CREATE INDEX "IX_user_refresh_tokens_replaced_by_token_id" ON user_refresh_tokens (replaced_by_token_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260521091145_AddUserRefreshTokens') THEN
    CREATE INDEX "IX_user_refresh_tokens_revoked_at" ON user_refresh_tokens (revoked_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260521091145_AddUserRefreshTokens') THEN
    CREATE UNIQUE INDEX "IX_user_refresh_tokens_token_hash" ON user_refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260521091145_AddUserRefreshTokens') THEN
    CREATE INDEX "IX_user_refresh_tokens_user_id" ON user_refresh_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260521091145_AddUserRefreshTokens') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260521091145_AddUserRefreshTokens', '10.0.0');
    END IF;
END $EF$;
COMMIT;

