using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;

namespace Infrastucture.Configurations;

public static class AwsSecretsConfigurationExtensions
{
    private const string RegionKey = "AWS_REGION";
    private const string SecretNameKey = "AWS_SECRETS_MANAGER_SECRET_NAME";

    public static async Task AddSecretsIfProductionAsync(
        this ConfigurationManager configuration,
        bool isProduction,
        CancellationToken ct = default)
    {
        if (!isProduction)
        {
            return;
        }

        var region = Required(configuration, RegionKey, "AWS region is not configured.");
        var secretName = Required(configuration, SecretNameKey, "AWS Secrets Manager secret name is not configured.");

        using var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
        var response = await client.GetSecretValueAsync(
            new GetSecretValueRequest
            {
                SecretId = secretName,
                VersionStage = "AWSCURRENT"
            },
            ct);

        if (string.IsNullOrWhiteSpace(response.SecretString))
        {
            throw new InvalidOperationException("AWS Secrets Manager secret string is empty.");
        }

        configuration.AddInMemoryCollection(ParseSecret(response.SecretString));
    }

    private static IReadOnlyDictionary<string, string?> ParseSecret(string secretJson)
    {
        using var document = JsonDocument.Parse(secretJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("AWS Secrets Manager secret must be a JSON object.");
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            values[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.GetRawText();
        }

        return values;
    }

    private static string Required(
        IConfiguration configuration,
        string key,
        string errorMessage)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return value;
    }
}
