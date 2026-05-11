using Domain.Features.Auth.Entities;
using Domain.Features.Links.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastucture.Database.Configurations;

public sealed class LinkConfiguration : IEntityTypeConfiguration<Link>
{
    public void Configure(EntityTypeBuilder<Link> builder)
    {
        builder.ToTable("links");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.OwnerId)
            .HasColumnName("owner_id");

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(6);

        builder.Property(x => x.DestinationUrl)
            .HasColumnName("destination_url")
            .HasColumnType("text");

        builder.Property(x => x.ClickCount)
            .HasColumnName("click_count");

        builder.Property(x => x.LastClickedAt)
            .HasColumnName("last_clicked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.IsDisabled)
            .HasColumnName("is_disabled");

        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.HasIndex(x => x.OwnerId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
