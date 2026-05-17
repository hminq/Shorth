using Amazon;
using Amazon.S3;
using Amazon.SQS;
using Application.Features.Auth.Interfaces;
using Application.Features.Links.Interfaces;
using Application.Features.Outbox.Interfaces;
using Application.Features.Upload.Interfaces;
using Domain.Features.Auth.Enums;
using Domain.Features.Outbox.Enums;
using Infrastucture.Configurations;
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
        var databaseOptions = ReadDatabaseOptions(configuration);
        var redisOptions = ReadRedisOptions(configuration);
        var awsOptions = ReadAwsOptions(configuration);
        var sqsOptions = ReadSqsOptions(configuration);
        var s3Options = ReadS3Options(configuration);
        var resendOptions = ReadResendOptions(configuration);
        var googleAuthOptions = ReadGoogleAuthOptions(configuration);
        var emailOptions = ReadEmailOptions(configuration);
        var jwtOptions = ReadJwtOptions(configuration);
        var turnstileOptions = ReadTurnstileOptions(configuration);

        services.AddSingleton(databaseOptions);
        services.AddSingleton(redisOptions);
        services.AddSingleton(awsOptions);
        services.AddSingleton(sqsOptions);
        services.AddSingleton(s3Options);
        services.AddSingleton(resendOptions);
        services.AddSingleton(googleAuthOptions);
        services.AddSingleton(emailOptions);
        services.AddSingleton(jwtOptions);
        services.AddSingleton(turnstileOptions);

        services
            .AddDatabase(databaseOptions)
            .AddRedisCaching(redisOptions)
            .AddAwsClients(awsOptions)
            .AddEmailInfrastructure(resendOptions)
            .AddLinkInfrastructure()
            .AddAuthInfrastructure()
            .AddUploadInfrastructure();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        DatabaseOptions options)
    {
        services.AddDbContext<AppDbContext>(dbContextOptions =>
            dbContextOptions.UseNpgsql(options.ConnectionString, npgsqlOptions =>
            {
                npgsqlOptions.MapEnum<IdentityProvider>("identity_provider");
                npgsqlOptions.MapEnum<UserStatus>("user_status");
                npgsqlOptions.MapEnum<OtpPurpose>("otp_purpose");
                npgsqlOptions.MapEnum<OutboxMessageType>("outbox_message_type");
                npgsqlOptions.MapEnum<OutboxMessageStatus>("outbox_message_status");
            }));

        return services;
    }

    private static IServiceCollection AddRedisCaching(
        this IServiceCollection services,
        RedisOptions options)
    {
        services.AddStackExchangeRedisCache(cacheOptions =>
        {
            cacheOptions.Configuration = options.ConnectionString;
            cacheOptions.InstanceName = options.InstanceName;
        });

        return services;
    }

    private static IServiceCollection AddAwsClients(
        this IServiceCollection services,
        AwsOptions options)
    {
        var region = RegionEndpoint.GetBySystemName(options.Region);

        services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(region));
        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(region));

        return services;
    }

    private static IServiceCollection AddEmailInfrastructure(
        this IServiceCollection services,
        ResendOptions options)
    {
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(resendOptions =>
        {
            resendOptions.ApiToken = options.ApiKey;
        });
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IEmailService, ResendEmailService>();

        return services;
    }

    private static IServiceCollection AddLinkInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<ILinkClickEventRepository, LinkClickEventRepository>();
        services.AddScoped<ILinkCacheRepository>(provider =>
            new LinkCacheRepository(
                provider.GetRequiredService<IDistributedCache>(),
                TimeSpan.FromHours(provider.GetRequiredService<RedisOptions>().LinkDestinationUrlTtlHours)));
        services.AddScoped<ISlugGenerator, SlugGenerator>();
        services.AddScoped<IClickEventQueue, SqsClickEventQueue>();
        services.AddHttpClient<ICaptchaVerifier, CloudflareTurnstileVerifier>();

        return services;
    }

    private static IServiceCollection AddAuthInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserIdentityRepository, UserIdentityRepository>();
        services.AddScoped<IUserOtpRepository, UserOtpRepository>();
        services.AddScoped<ILocalRegistrationRepository, LocalRegistrationRepository>();
        services.AddScoped<IExternalIdentityRepository, ExternalIdentityRepository>();
        services.AddScoped<IOtpCodeGenerator, RandomOtpCodeGenerator>();
        services.AddScoped<IGoogleAuthProvider, GoogleAuthProvider>();
        services.AddScoped<IGoogleAuthStateRepository>(provider =>
            new GoogleAuthStateRepository(
                provider.GetRequiredService<IDistributedCache>(),
                TimeSpan.FromMinutes(provider.GetRequiredService<GoogleAuthOptions>().StateTtlMinutes)));
        services.AddScoped<IEmailJobQueue, SqsEmailJobQueue>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        return services;
    }

    private static IServiceCollection AddUploadInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUploadPresignService, S3UploadPresignService>();

        return services;
    }

    private static DatabaseOptions ReadDatabaseOptions(IConfiguration configuration)
    {
        return new DatabaseOptions(
            Required(configuration, "DB_CONNECTION_STRING", "DB connection string is not configured."));
    }

    private static RedisOptions ReadRedisOptions(IConfiguration configuration)
    {
        return new RedisOptions(
            Required(configuration, "REDIS_CONNECTION_STRING", "Redis connection string is not configured."),
            Required(configuration, "REDIS_INSTANCE_NAME", "Redis instance name is not configured."),
            PositiveInt(
                configuration,
                "REDIS_LINK_DESTINATION_URL_TTL_HOURS",
                "Redis link ttl is not configured.",
                "Redis link ttl hours must be a valid positive integer."));
    }

    private static AwsOptions ReadAwsOptions(IConfiguration configuration)
    {
        return new AwsOptions(
            Required(configuration, "AWS_REGION", "AWS region is not configured."));
    }

    private static SqsOptions ReadSqsOptions(IConfiguration configuration)
    {
        return new SqsOptions(
            Required(configuration, "SQS_EMAIL_JOBS_QUEUE_URL", "SQS email jobs queue url is not configured."),
            Required(configuration, "SQS_CLICK_EVENTS_QUEUE_URL", "SQS click events queue url is not configured."));
    }

    private static S3Options ReadS3Options(IConfiguration configuration)
    {
        return new S3Options(
            Required(configuration, "S3_BUCKET_NAME", "S3 bucket name is not configured."),
            Required(configuration, "S3_PUBLIC_BASE_URL", "S3 public base url is not configured."));
    }

    private static ResendOptions ReadResendOptions(IConfiguration configuration)
    {
        return new ResendOptions(
            Required(configuration, "RESEND_API_KEY", "Resend API key is not configured."));
    }

    private static GoogleAuthOptions ReadGoogleAuthOptions(IConfiguration configuration)
    {
        return new GoogleAuthOptions(
            Required(configuration, "GOOGLE_CLIENT_ID", "Google client id is not configured."),
            Required(configuration, "GOOGLE_CLIENT_SECRET", "Google client secret is not configured."),
            Required(configuration, "GOOGLE_OAUTH_REDIRECT_URI", "Google oauth redirect uri is not configured."),
            PositiveInt(
                configuration,
                "GOOGLE_AUTH_STATE_TTL_MINUTES",
                "Google auth state ttl minutes is not configured.",
                "Google auth state ttl minutes must be a valid positive integer."));
    }

    private static EmailOptions ReadEmailOptions(IConfiguration configuration)
    {
        return new EmailOptions(
            Required(configuration, "EMAIL_FROM_ADDRESS", "Email from address is not configured."),
            configuration["EMAIL_FROM_NAME"] ?? "Short",
            Required(configuration, "EMAIL_LOGO_URL", "Email logo url is not configured."),
            configuration["EMAIL_PROJECT_NAME"] ?? "Short");
    }

    private static JwtOptions ReadJwtOptions(IConfiguration configuration)
    {
        var signingKey = Required(configuration, "JWT_SIGNING_KEY", "JWT signing key is not configured.");
        if (signingKey.Length < 32)
        {
            throw new InvalidOperationException("JWT signing key must be at least 32 characters.");
        }

        return new JwtOptions(
            signingKey,
            PositiveInt(
                configuration,
                "JWT_ACCESS_TOKEN_TTL_MINUTES",
                "JWT access token ttl minutes is not configured.",
                "JWT access token ttl minutes must be a valid positive integer."));
    }

    private static TurnstileOptions ReadTurnstileOptions(IConfiguration configuration)
    {
        return new TurnstileOptions(
            Required(configuration, "TURNSTILE_SECRET_KEY", "Turnstile secret key is not configured."));
    }

    private static string Required(
        IConfiguration configuration,
        string key,
        string errorMessage)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return value;
    }

    private static int PositiveInt(
        IConfiguration configuration,
        string key,
        string missingErrorMessage,
        string invalidErrorMessage)
    {
        var value = Required(configuration, key, missingErrorMessage);
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
        {
            throw new InvalidOperationException(invalidErrorMessage);
        }

        return parsed;
    }
}
