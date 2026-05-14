using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Application.Features.Auth.Interfaces;
using Application.Features.Auth.Messages;
using Infrastucture.Configurations;

namespace Worker;

public sealed class EmailJobWorker : BackgroundService
{
    private const int MaxMessagesPerPoll = 10;
    private const int LongPollSeconds = 20;
    private static readonly TimeSpan PollFailureDelay = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<EmailJobWorker> _logger;
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _queueUrl;

    public EmailJobWorker(
        ILogger<EmailJobWorker> logger,
        IAmazonSQS sqsClient,
        IServiceScopeFactory scopeFactory,
        SqsOptions options)
    {
        _logger = logger;
        _sqsClient = sqsClient;
        _scopeFactory = scopeFactory;
        _queueUrl = options.EmailJobsQueueUrl;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Email job worker started. QueueUrl: {QueueUrl}, MaxMessagesPerPoll: {MaxMessagesPerPoll}, LongPollSeconds: {LongPollSeconds}",
            _queueUrl,
            MaxMessagesPerPoll,
            LongPollSeconds);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Polling email jobs queue.");

                var response = await _sqsClient.ReceiveMessageAsync(
                    new ReceiveMessageRequest
                    {
                        QueueUrl = _queueUrl,
                        MaxNumberOfMessages = MaxMessagesPerPoll,
                        WaitTimeSeconds = LongPollSeconds
                    },
                    ct);

                if (response.Messages is null || response.Messages.Count == 0)
                {
                    _logger.LogDebug("No email job messages received.");
                    continue;
                }

                foreach (var message in response.Messages)
                {
                    await ProcessMessageAsync(message, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to poll email jobs queue.");
                await Task.Delay(PollFailureDelay, ct);
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
            await using var scope = _scopeFactory.CreateAsyncScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            await emailService.SendAsync(emailJob, ct);
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
                QueueUrl = _queueUrl,
                ReceiptHandle = sqsMessage.ReceiptHandle
            },
            ct);
    }
}
