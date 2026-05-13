namespace Infrastucture.Configurations;

public sealed record RedisOptions(
    string ConnectionString,
    string InstanceName,
    int LinkDestinationUrlTtlHours);
