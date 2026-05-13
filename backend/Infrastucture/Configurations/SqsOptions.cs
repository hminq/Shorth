namespace Infrastucture.Configurations;

public sealed record SqsOptions(
    string EmailJobsQueueUrl,
    string ClickEventsQueueUrl);
