using Infrastucture;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<global::Worker.Worker>();

var host = builder.Build();
host.Run();
