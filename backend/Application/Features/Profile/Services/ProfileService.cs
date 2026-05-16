using Application.Features.Auth.Interfaces;
using Application.Features.Profile.Dtos;
using Domain.Common.Exceptions;
using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Enums;
using Domain.Features.Auth.Exceptions;

namespace Application.Features.Profile.Services;

public sealed class ProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ProfileService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserProfileResult> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await GetExistingUserAsync(userId, ct);
        var identities = await _userIdentityRepository.GetByUserIdAsync(user.Id, ct);
        var hasPassword = identities.Any(x =>
            x.Provider == IdentityProvider.Local
            && !string.IsNullOrWhiteSpace(x.PasswordHash));

        return ToResult(user, hasPassword);
    }

    public async Task<UserProfileResult> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await GetExistingUserAsync(request.UserId, ct);
        var identities = await _userIdentityRepository.GetByUserIdAsync(user.Id, ct);
        var localIdentity = identities.FirstOrDefault(x => x.Provider == IdentityProvider.Local);
        var shouldAddLocalIdentity = false;
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            if (localIdentity is not null)
            {
                if (string.IsNullOrWhiteSpace(request.CurrentPassword)
                    || string.IsNullOrWhiteSpace(localIdentity.PasswordHash)
                    || !_passwordHasher.Verify(request.CurrentPassword, localIdentity.PasswordHash))
                {
                    throw new WrongCredentialsException("Current password is incorrect.");
                }

                var passwordHash = _passwordHasher.Hash(request.NewPassword);
                localIdentity.UpdatePasswordHash(passwordHash, now);
            }
            else
            {
                if (!user.EmailVerifiedAt.HasValue)
                {
                    throw new EmailVerificationRequiredException("A verified email is required before setting a password.");
                }

                if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.EmailNormalized))
                {
                    throw new DomainException("A normalized email is required before setting a password.");
                }

                var passwordHash = _passwordHasher.Hash(request.NewPassword);
                localIdentity = UserIdentity.CreateLocal(
                    user.Id,
                    user.Email,
                    user.EmailNormalized,
                    passwordHash,
                    now);
                shouldAddLocalIdentity = true;
            }
        }

        user.UpdateProfile(
            request.DisplayName ?? user.DisplayName,
            request.AvatarUrl ?? user.AvatarUrl,
            now);

        if (localIdentity is not null && shouldAddLocalIdentity)
        {
            await _userRepository.UpdateWithNewIdentityAsync(user, localIdentity, ct);
        }
        else
        {
            await _userRepository.UpdateAsync(user, ct);
        }

        return ToResult(user, localIdentity is not null);
    }

    private async Task<User> GetExistingUserAsync(Guid userId, CancellationToken ct)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
        {
            throw new UnauthorizedAccessException("The authenticated user does not exist.");
        }

        if (user.Status == UserStatus.Disabled)
        {
            throw new AccountDisableException("This account is disabled.");
        }

        return user;
    }

    private static UserProfileResult ToResult(User user, bool hasPassword)
    {
        return new UserProfileResult(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName ?? string.Empty,
            user.AvatarUrl,
            hasPassword);
    }
}
