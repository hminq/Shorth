using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Application.Features.Auth.Interfaces;
using Application.Features.Auth.Messages;
using Microsoft.Extensions.Options;

namespace Worker;

public sealed class EmailJobWorker : BackgroundService
{
    private const int MaxMessagesPerPoll = 10;
    private const int LongPollSeconds = 20;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<EmailJobWorker> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly IEmailService _emailService;
    private readonly EmailWorkerOptions _options;

    public EmailJobWorker(
        ILogger<EmailJobWorker> logger,
        IAmazonSQS sqsClient,
        IEmailService emailService,
        IOptions<EmailWorkerOptions> options)
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _emailService = emailService;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var response = await _sqsClient.ReceiveMessageAsync(
                new ReceiveMessageRequest
                {
                    QueueUrl = _options.QueueUrl,
                    MaxNumberOfMessages = MaxMessagesPerPoll,
                    WaitTimeSeconds = LongPollSeconds
                },
                ct);

            if (response.Messages.Count == 0)
            {
                continue;
            }

            foreach (var message in response.Messages)
            {
                await ProcessMessageAsync(message, ct);
            }
        }
    }

    private async Task ProcessMessageAsync(Message sqsMessage, CancellationToken ct)
    {
        EmailJobMessage? emailJob;

        try
        {
            emailJob = JsonSerializer.Deserialize<EmailJobMessage>(sqsMessage.Body, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize email job message. MessageId: {MessageId}", sqsMessage.MessageId);
            await DeleteMessageAsync(sqsMessage, ct);
            return;
        }

        if (emailJob is null)
        {
            _logger.LogWarning("Email job message body was empty after deserialization. MessageId: {MessageId}", sqsMessage.MessageId);
            await DeleteMessageAsync(sqsMessage, ct);
            return;
        }

        try
        {
            await _emailService.SendAsync(emailJob, ct);
            await DeleteMessageAsync(sqsMessage, ct);

            _logger.LogInformation(
                "Processed email job {Type} for user {UserId}. MessageId: {MessageId}",
                emailJob.Type,
                emailJob.UserId,
                sqsMessage.MessageId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Email job payload was invalid for user {UserId}. MessageId: {MessageId}",
                emailJob.UserId,
                sqsMessage.MessageId);

            await DeleteMessageAsync(sqsMessage, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process email job {Type} for user {UserId}. MessageId: {MessageId}",
                emailJob.Type,
                emailJob.UserId,
                sqsMessage.MessageId);
        }
    }

    private async Task DeleteMessageAsync(Message sqsMessage, CancellationToken ct)
    {
        await _sqsClient.DeleteMessageAsync(
            new DeleteMessageRequest
            {
                QueueUrl = _options.QueueUrl,
                ReceiptHandle = sqsMessage.ReceiptHandle
            },
            ct);
    }
}
