using Api.Exceptions;
using Infrastucture.Configurations;
using Infrastucture;
using Api.HealthChecks;
using Application.Features.Auth.Services;
using Application.Features.Links.Services;
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
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var signingKey = builder.Configuration["JWT_SIGNING_KEY"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
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

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// health check endpoints
app.MapHealthChecks("/health");

// ping endpoint
app.MapGet("/ping", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();
