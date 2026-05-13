using Infrastucture.Configurations;
using Infrastucture;
using Worker;

var builder = Host.CreateApplicationBuilder(args);
await builder.Configuration.AddSecretsIfProductionAsync(builder.Environment.IsProduction());

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<EmailJobWorker>();

var host = builder.Build();
host.Run();
