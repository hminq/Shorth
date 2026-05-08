using Infrastucture;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// ping endpoint
app.MapGet("/ping", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();
