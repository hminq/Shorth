using Application.Features.Auth.Messages;

namespace Application.Features.Auth.Interfaces;

public interface IEmailJobQueue
{
    Task EnqueueAsync(EmailJobMessage message, CancellationToken ct = default);
}
