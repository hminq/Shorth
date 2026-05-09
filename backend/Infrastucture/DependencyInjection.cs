using Application.Abstractions;
using Infrastucture.Database;
using Infrastucture.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastucture;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection is not configured.");
        }

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            throw new InvalidOperationException("Redis connection string is not configured.");
        }

        var redisInstanceName = configuration["Redis:InstanceName"];
        if (string.IsNullOrWhiteSpace(redisInstanceName))
        {
            throw new InvalidOperationException("Redis instance name is not configured.");
        }

        var redisLinkTtlHour = configuration["Redis:LinkDestinationUrlTtlHours"];
        if (string.IsNullOrWhiteSpace(redisLinkTtlHour))
        {
            throw new InvalidOperationException("Redis link ttl is not configured.");
        }

        if (!int.TryParse(redisLinkTtlHour, out var redisLinkTtlHours) || redisLinkTtlHours <= 0)
        {
            throw new InvalidOperationException("Redis link ttl hours must be a valid positive integer.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = redisInstanceName;
        });

        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<ILinkCacheRepository>(_ =>
            new LinkCacheRepository(
                _.GetRequiredService<IDistributedCache>(),
                TimeSpan.FromHours(redisLinkTtlHours)));
        services.AddScoped<ISlugGenerator, SlugGenerator>();

        return services;
    }
}
