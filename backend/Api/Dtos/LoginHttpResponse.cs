using System;

namespace Api.Dtos;

public sealed record LoginHttpResponse(
    string AccessToken,
    Guid UserId,
    string Email,
    string DisplayName
);
