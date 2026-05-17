using System;
using Domain.Features.Outbox.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastucture.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:identity_provider", "local,google,github")
                .Annotation("Npgsql:Enum:otp_purpose", "email_verification,password_reset")
                .Annotation("Npgsql:Enum:outbox_message_status", "pending,processing,processed,failed")
                .Annotation("Npgsql:Enum:outbox_message_type", "email_job")
                .Annotation("Npgsql:Enum:user_status", "pending_verification,active,disabled")
                .OldAnnotation("Npgsql:Enum:identity_provider", "local,google,github")
                .OldAnnotation("Npgsql:Enum:otp_purpose", "email_verification,password_reset")
                .OldAnnotation("Npgsql:Enum:user_status", "pending_verification,active,disabled");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<OutboxMessageType>(type: "outbox_message_type", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<OutboxMessageStatus>(type: "outbox_message_status", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                    table.CheckConstraint("ck_outbox_messages_retry_count_range", "retry_count >= 0 AND retry_count <= 10");
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_status_next_attempt_at",
                table: "outbox_messages",
                columns: new[] { "status", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:identity_provider", "local,google,github")
                .Annotation("Npgsql:Enum:otp_purpose", "email_verification,password_reset")
                .Annotation("Npgsql:Enum:user_status", "pending_verification,active,disabled")
                .OldAnnotation("Npgsql:Enum:identity_provider", "local,google,github")
                .OldAnnotation("Npgsql:Enum:otp_purpose", "email_verification,password_reset")
                .OldAnnotation("Npgsql:Enum:outbox_message_status", "pending,processing,processed,failed")
                .OldAnnotation("Npgsql:Enum:outbox_message_type", "email_job")
                .OldAnnotation("Npgsql:Enum:user_status", "pending_verification,active,disabled");
        }
    }
}
