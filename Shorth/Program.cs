var builder = WebApplication.CreateBuilder(args);

// get database connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add services to the container.
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// ping endpoint
app.MapGet("/ping", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();
