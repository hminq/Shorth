using Domain.Features.Auth.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastucture.Database.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(320);

        builder.Property(x => x.EmailNormalized)
            .HasColumnName("email_normalized")
            .HasMaxLength(320);

        builder.Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100);

        builder.Property(x => x.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(500);

        builder.Property(x => x.EmailVerifiedAt)
            .HasColumnName("email_verified_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("user_status");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.EmailNormalized)
            .IsUnique()
            .HasFilter("\"email_normalized\" IS NOT NULL");
    }
}
