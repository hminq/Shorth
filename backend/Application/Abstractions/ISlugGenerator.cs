namespace Application.Abstractions;

public interface ISlugGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}
