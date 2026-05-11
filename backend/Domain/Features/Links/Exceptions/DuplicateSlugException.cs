using Domain.Common.Exceptions;

namespace Domain.Features.Links.Exceptions;

public sealed class DuplicateSlugException(string message) : DomainException(message);
