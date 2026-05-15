namespace Application.Features.Links.Interfaces;

public interface ICaptchaVerifier
{
    Task<bool> VerifyAsync(string token, CancellationToken ct = default);
}
