using Application.Abstractions;
using Amazon;
using Amazon.S3;
using Amazon.SQS;
using Infrastucture.Database;
using Infrastucture.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Resend;

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

        var awsRegion = configuration["AWS_REGION"];
        if (string.IsNullOrWhiteSpace(awsRegion))
        {
            throw new InvalidOperationException("AWS region is not configured.");
        }

        var sqsQueueUrl = configuration["Sqs:QueueUrl"];
        if (string.IsNullOrWhiteSpace(sqsQueueUrl))
        {
            throw new InvalidOperationException("SQS queue url is not configured.");
        }

        var s3BucketName = configuration["S3:BucketName"];
        if (string.IsNullOrWhiteSpace(s3BucketName))
        {
            throw new InvalidOperationException("S3 bucket name is not configured.");
        }

        var resendApiKey = configuration["RESEND_API_KEY"];
        if (string.IsNullOrWhiteSpace(resendApiKey))
        {
            throw new InvalidOperationException("Resend API key is not configured.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = redisInstanceName;
        });

        services.AddSingleton<IAmazonSQS>(_ =>
            new AmazonSQSClient(RegionEndpoint.GetBySystemName(awsRegion)));
        services.AddSingleton<IAmazonS3>(_ =>
            new AmazonS3Client(RegionEndpoint.GetBySystemName(awsRegion)));

        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = resendApiKey;
        });
        services.AddTransient<IResend, ResendClient>();

        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<ILinkCacheRepository>(_ =>
            new LinkCacheRepository(
                _.GetRequiredService<IDistributedCache>(),
                TimeSpan.FromHours(redisLinkTtlHours)));
        services.AddScoped<ISlugGenerator, SlugGenerator>();

        return services;
    }
}
