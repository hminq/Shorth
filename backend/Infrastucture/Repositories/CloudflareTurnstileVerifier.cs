using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Application.Features.Links.Interfaces;
using Infrastucture.Configurations;

namespace Infrastucture.Repositories;

public sealed class CloudflareTurnstileVerifier : ICaptchaVerifier
{
    private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

    private readonly HttpClient _httpClient;
    private readonly TurnstileOptions _options;

    public CloudflareTurnstileVerifier(HttpClient httpClient, TurnstileOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<bool> VerifyAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        using var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("secret", _options.SecretKey),
            new KeyValuePair<string, string>("response", token)
        ]);

        var response = await _httpClient.PostAsync(SiteVerifyUrl, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var payload = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(cancellationToken: ct);
        return payload?.Success == true;
    }

    private sealed record TurnstileVerifyResponse(
        [property: JsonPropertyName("success")] bool Success);
}
