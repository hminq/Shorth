using Api.Exceptions;
using Infrastucture;
using Api.HealthChecks;
using Application.UseCases;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<S3HealthCheck>("s3")
    .AddCheck<SqsHealthCheck>("sqs")
    .AddCheck<ResendHealthCheck>("resend");
builder.Services.AddScoped<CreateShortLink>();
builder.Services.AddScoped<ResolveShortLink>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

// ping endpoint
app.MapGet("/ping", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();
