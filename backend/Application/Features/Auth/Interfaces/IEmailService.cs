using Application.Features.Auth.Messages;

namespace Application.Features.Auth.Interfaces;

public interface IEmailService
{
    Task SendAsync(EmailJobMessage message, CancellationToken ct = default);
}
