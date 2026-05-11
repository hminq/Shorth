using System;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.Interfaces;
using Application.Features.Auth.Messages;
using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Enums;
using Domain.Features.Auth.Exceptions;

namespace Application.Features.Auth.UseCases;

public sealed class LocalRegisterUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly ILocalRegistrationRepository _localRegistrationRepository;
    private readonly IEmailJobQueue _emailJobQueue;
    private readonly IPasswordHasher _passwordHasher;

    public LocalRegisterUseCase(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        ILocalRegistrationRepository localRegistrationRepository,
        IEmailJobQueue emailJobQueue,
        IPasswordHasher passwordHasher
    )
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _localRegistrationRepository = localRegistrationRepository;
        _emailJobQueue = emailJobQueue;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterResult> ExecuteAsync(LocalRegisterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required.", nameof(request));
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var email = request.Email.Trim();
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? string.Empty : request.DisplayName.Trim();

        // check if email has existed
        var foundUser = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail, ct);

        // if email already existed
        if (foundUser is not null)
        {
            // check the identity of that user
            var identities = await _userIdentityRepository.GetByUserIdAsync(foundUser.Id, ct);

            if (identities.Any(x => x.Provider == IdentityProvider.Local))
            {
                throw new EmailAlreadyExistedException("This email already has an account.");
            }

            // if user has a verified email account but registered with another method
            if (identities.Any(x => x.Provider != IdentityProvider.Local))
            {
                throw new AlternateSignInRequiredException(
                    "This email address uses another sign-in method. Continue with that provider or set a password first.");
            }

            throw new InvalidOperationException("Something is wrong with your account.");
        }

        var createdAt = DateTime.UtcNow;
        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.CreateLocal(email, normalizedEmail, displayName, createdAt);
        var localIdentity = UserIdentity.CreateLocal(
            user.Id,
            email,
            normalizedEmail,
            passwordHash,
            createdAt);

        // Persist the user and local identity in a single transaction.
        await _localRegistrationRepository.CreateAsync(user, localIdentity, ct);

        // Enqueue the verification email after the transaction commits.
        await _emailJobQueue.EnqueueAsync(
            new EmailJobMessage(
                EmailJobType.VerifyEmail,
                user.Id,
                email,
                user.DisplayName,
                OtpCode: null,
                EnqueuedAtUtc: DateTime.UtcNow),
            ct);

        return new RegisterResult(
            user.Id,
            email,
            user.DisplayName ?? string.Empty,
            RequiresEmailVerification: true);
    }
}
