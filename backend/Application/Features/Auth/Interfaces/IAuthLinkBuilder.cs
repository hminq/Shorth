namespace Application.Features.Auth.Interfaces;

public interface IAuthLinkBuilder
{
    string BuildPasswordResetUrl(string email);
}
