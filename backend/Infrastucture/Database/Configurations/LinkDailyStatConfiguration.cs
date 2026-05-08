using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastucture.Database.Configurations;

public sealed class LinkDailyStatConfiguration : IEntityTypeConfiguration<LinkDailyStat>
{
    public void Configure(EntityTypeBuilder<LinkDailyStat> builder)
    {
        builder.ToTable("link_daily_stats");

        builder.HasKey(x => new { x.LinkId, x.Date });

        builder.Property(x => x.LinkId)
            .HasColumnName("link_id");

        builder.Property(x => x.Date)
            .HasColumnName("date");

        builder.Property(x => x.Clicks)
            .HasColumnName("clicks");

        builder.Property(x => x.UniqueVisitors)
            .HasColumnName("unique_visitors");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne<Link>()
            .WithMany()
            .HasForeignKey(x => x.LinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
