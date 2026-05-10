using System;
using Application.Abstractions;
using Application.Dtos;
using Domain.Entities.Enums;
using Domain.Exceptions;

namespace Application.UseCases;

public class LocalLogin
{
    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public LocalLogin(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher
    )
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResult> ExecuteAsync(LocalLoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required.", nameof(request));
        }

        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        // find user identity with this email
        var foundLocalIdentity = await _userIdentityRepository.GetLocalByEmailNormalizedAsync(emailNormalized, ct);

        // no identity found
        if (foundLocalIdentity is null)
        {
            // check if has user with this email
            var existingUser = await _userRepository.GetByEmailNormalizedAsync(emailNormalized, ct);

            // no user with email found
            if (existingUser is null)
            {
                throw new WrongCredentialsException("Wrong credentials.");
            }

            // found user with email -> find identities with that user
            var identities = await _userIdentityRepository.GetByUserIdAsync(existingUser.Id, ct);

            // if there is no LocalIdentity with this user's account
            if (identities.Any(x => x.Provider != IdentityProvider.Local))
            {
                throw new AlternateSignInRequiredException(
                    "This email address uses another sign-in method. Continue with that provider or set a password first.");
            }

            // no Identity but still has User 
            throw new InvalidOperationException("Something is wrong with your account.");
        }

        // check passwordhash
        if (string.IsNullOrWhiteSpace(foundLocalIdentity.PasswordHash))
        {
            throw new InvalidOperationException("Something is wrong with your account.");
        }

        // verify password
        if (!_passwordHasher.Verify(request.Password, foundLocalIdentity.PasswordHash))
        {
            throw new WrongCredentialsException("Wrong credentials.");
        }

        // get user with correct credentials (identity)
        var foundUser = await _userRepository.GetByIdAsync(foundLocalIdentity.UserId, ct);
        if (foundUser is null)
        {
            throw new InvalidOperationException("Something is wrong with your account.");
        }

        // check if this account dis disabled
        if (foundUser.Status == UserStatus.Disabled)
        {
            throw new AccountDisableException("This account is disabled.");
        }
        
        // update login time 
        foundUser.MarkLastLogin(DateTime.UtcNow);
        await _userRepository.UpdateAsync(foundUser, ct);
        
        var accessToken = _jwtTokenGenerator.GenerateToken(foundUser);

        return new LoginResult(
            accessToken,
            foundUser.Id,
            foundUser.Email ?? string.Empty,
            foundUser.DisplayName ?? string.Empty
        );
    }
}
