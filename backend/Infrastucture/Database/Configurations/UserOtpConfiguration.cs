using Domain.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastucture.Database.Configurations;

public sealed class UserOtpConfiguration : IEntityTypeConfiguration<UserOtp>
{
    public void Configure(EntityTypeBuilder<UserOtp> builder)
    {
        builder.ToTable("user_otps");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.Purpose)
            .HasColumnName("purpose")
            .HasColumnType("otp_purpose");

        builder.Property(x => x.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(255);

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UsedAt)
            .HasColumnName("used_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.InvalidatedAt)
            .HasColumnName("invalidated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.AttemptCount)
            .HasColumnName("attempt_count");

        builder.Property(x => x.MaxAttempts)
            .HasColumnName("max_attempts");

        builder.Property(x => x.LastSentAt)
            .HasColumnName("last_sent_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.UserId, x.Purpose });
        builder.HasIndex(x => x.ExpiresAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
