using System;

namespace Api.Features.Auth.Dtos;

public sealed record LoginHttpResponse(
    string AccessToken,
    Guid UserId,
    string Email,
    string DisplayName
);
