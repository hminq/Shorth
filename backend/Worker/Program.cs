using Infrastucture;
using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOptions<EmailWorkerOptions>()
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.QueueUrl = configuration["SQS_EMAIL_JOBS_QUEUE_URL"]
            ?? throw new InvalidOperationException("SQS email jobs queue url is not configured.");
    })
    .Validate(options => Uri.IsWellFormedUriString(options.QueueUrl, UriKind.Absolute), "Email queue url must be a valid absolute url.")
    .ValidateOnStart();
builder.Services.AddHostedService<EmailJobWorker>();

var host = builder.Build();
host.Run();
