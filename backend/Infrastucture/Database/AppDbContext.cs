using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Enums;
using Domain.Features.Links.Entities;
using Domain.Features.Outbox.Entities;
using Domain.Features.Outbox.Enums;
using Infrastucture.Database.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Infrastucture.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<UserOtp> UserOtps => Set<UserOtp>();
    public DbSet<Link> Links => Set<Link>();
    public DbSet<LinkClickEvent> LinkClickEvents => Set<LinkClickEvent>();
    public DbSet<LinkDailyStat> LinkDailyStats => Set<LinkDailyStat>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<UserStatus>(name: "user_status");
        modelBuilder.HasPostgresEnum<IdentityProvider>(name: "identity_provider");
        modelBuilder.HasPostgresEnum<OtpPurpose>(name: "otp_purpose");
        modelBuilder.HasPostgresEnum<OutboxMessageType>(name: "outbox_message_type");
        modelBuilder.HasPostgresEnum<OutboxMessageStatus>(name: "outbox_message_status");

        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserIdentityConfiguration());
        modelBuilder.ApplyConfiguration(new UserOtpConfiguration());
        modelBuilder.ApplyConfiguration(new LinkConfiguration());
        modelBuilder.ApplyConfiguration(new LinkClickEventConfiguration());
        modelBuilder.ApplyConfiguration(new LinkDailyStatConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
