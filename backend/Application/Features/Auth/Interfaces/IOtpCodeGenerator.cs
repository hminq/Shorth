namespace Application.Features.Auth.Interfaces;

public interface IOtpCodeGenerator
{
    string GenerateNumericCode(int length);
}
