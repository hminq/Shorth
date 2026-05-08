using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastucture.Database.Configurations;

public sealed class LinkClickEventConfiguration : IEntityTypeConfiguration<LinkClickEvent>
{
    public void Configure(EntityTypeBuilder<LinkClickEvent> builder)
    {
        builder.ToTable("link_click_events");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.LinkId)
            .HasColumnName("link_id");

        builder.Property(x => x.ClickedAt)
            .HasColumnName("clicked_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text");

        builder.Property(x => x.Referrer)
            .HasColumnName("referrer")
            .HasColumnType("text");

        builder.Property(x => x.IpHash)
            .HasColumnName("ip_hash")
            .HasMaxLength(128);

        builder.Property(x => x.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(2);

        builder.Property(x => x.DeviceType)
            .HasColumnName("device_type")
            .HasMaxLength(50);

        builder.Property(x => x.BrowserFamily)
            .HasColumnName("browser_family")
            .HasMaxLength(100);

        builder.Property(x => x.OsFamily)
            .HasColumnName("os_family")
            .HasMaxLength(100);

        builder.HasIndex(x => x.LinkId);
        builder.HasIndex(x => new { x.LinkId, x.ClickedAt });

        builder.HasOne<Link>()
            .WithMany()
            .HasForeignKey(x => x.LinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
