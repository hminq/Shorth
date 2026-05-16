using Api.Exceptions;
using Api.Configurations;
using Infrastucture.Configurations;
using Infrastucture;
using Api.HealthChecks;
using Application.Features.Auth.Services;
using Application.Features.Links.Services;
using Application.Features.Profile.Services;
using Application.Features.Upload.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
await builder.Configuration.AddSecretsIfProductionAsync(builder.Environment.IsProduction());

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
var authCookieOptions = ReadAuthCookieOptions(builder.Configuration, builder.Environment.IsProduction());
builder.Services.AddSingleton(authCookieOptions);
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        policy
            .WithOrigins(authCookieOptions.WebBaseUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<JwtOptions, AuthCookieOptions>((options, jwtOptions, cookieOptions) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token)
                    && context.Request.Cookies.TryGetValue(cookieOptions.CookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<S3HealthCheck>("s3")
    .AddCheck<SqsHealthCheck>("sqs")
    .AddCheck<ResendHealthCheck>("resend");
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LinkService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<UploadService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

// ping endpoint
app.MapMethods("/ping", ["GET", "HEAD"], () => 
    Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();

static AuthCookieOptions ReadAuthCookieOptions(IConfiguration configuration, bool isProduction)
{
    var webBaseUrl = configuration["WEB_BASE_URL"];
    if (string.IsNullOrWhiteSpace(webBaseUrl))
    {
        if (isProduction)
        {
            throw new InvalidOperationException("Web base url is not configured.");
        }

        webBaseUrl = "http://localhost:5173";
    }

    var cookieName = configuration["AUTH_COOKIE_NAME"];
    if (string.IsNullOrWhiteSpace(cookieName))
    {
        cookieName = "shorth_access_token";
    }

    var accessTokenTtl = configuration["JWT_ACCESS_TOKEN_TTL_MINUTES"];
    if (!int.TryParse(accessTokenTtl, out var accessTokenTtlMinutes) || accessTokenTtlMinutes <= 0)
    {
        throw new InvalidOperationException("JWT access token ttl minutes must be a valid positive integer.");
    }

    return new AuthCookieOptions(cookieName, webBaseUrl.TrimEnd('/'), accessTokenTtlMinutes);
}
