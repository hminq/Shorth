using Application.Features.Auth.Dtos;
using Application.Features.Auth.Interfaces;
using Application.Features.Auth.Messages;
using System.Security.Cryptography;
using Domain.Features.Auth.Constants;
using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Enums;
using Domain.Features.Auth.Exceptions;

namespace Application.Features.Auth.Services;

public sealed class AuthService
{
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserOtpRepository _userOtpRepository;
    private readonly ILocalRegistrationRepository _localRegistrationRepository;
    private readonly IEmailJobQueue _emailJobQueue;
    private readonly IOtpCodeGenerator _otpCodeGenerator;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IGoogleAuthProvider _googleAuthProvider;
    private readonly IGoogleAuthStateRepository _googleAuthStateStore;

    public AuthService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IUserOtpRepository userOtpRepository,
        ILocalRegistrationRepository localRegistrationRepository,
        IEmailJobQueue emailJobQueue,
        IOtpCodeGenerator otpCodeGenerator,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher,
        IGoogleAuthProvider googleAuthProvider,
        IGoogleAuthStateRepository googleAuthStateStore)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _userOtpRepository = userOtpRepository;
        _localRegistrationRepository = localRegistrationRepository;
        _emailJobQueue = emailJobQueue;
        _otpCodeGenerator = otpCodeGenerator;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
        _googleAuthProvider = googleAuthProvider;
        _googleAuthStateStore = googleAuthStateStore;
    }

    public async Task<GoogleLoginUrlResult> GenerateGoogleLoginUrlAsync(CancellationToken ct = default)
    {
        var createdAt = DateTime.UtcNow;
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var authState = new GoogleAuthState(createdAt);

        await _googleAuthStateStore.StoreAsync(state, authState, ct);

        var authorizationUrl = _googleAuthProvider.BuildAuthorizationUrl(state);

        return new GoogleLoginUrlResult(authorizationUrl);
    }

    public async Task<LoginResult> LocalLoginAsync(LocalLoginRequest request, CancellationToken ct = default)
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
        var foundLocalIdentity = await _userIdentityRepository.GetLocalByEmailNormalizedAsync(emailNormalized, ct);

        if (foundLocalIdentity is null)
        {
            var existingUser = await _userRepository.GetByEmailNormalizedAsync(emailNormalized, ct);

            if (existingUser is null)
            {
                throw new WrongCredentialsException("Wrong credentials.");
            }

            var identities = await _userIdentityRepository.GetByUserIdAsync(existingUser.Id, ct);

            if (identities.Any(x => x.Provider != IdentityProvider.Local))
            {
                throw new AlternateSignInRequiredException(
                    "This email address uses another sign-in method. Continue with that provider or set a password first.");
            }

            throw new InvalidOperationException("Something is wrong with your account.");
        }

        if (string.IsNullOrWhiteSpace(foundLocalIdentity.PasswordHash))
        {
            throw new InvalidOperationException("Something is wrong with your account.");
        }

        if (!_passwordHasher.Verify(request.Password, foundLocalIdentity.PasswordHash))
        {
            throw new WrongCredentialsException("Wrong credentials.");
        }

        var foundUser = await _userRepository.GetByIdAsync(foundLocalIdentity.UserId, ct);
        if (foundUser is null)
        {
            throw new InvalidOperationException("Something is wrong with your account.");
        }

        if (foundUser.Status == UserStatus.Disabled)
        {
            throw new AccountDisableException("This account is disabled.");
        }

        if (!foundUser.EmailVerifiedAt.HasValue)
        {
            throw new EmailVerificationRequiredException(
                "This account requires email verification.");
        }

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

    public async Task<RegisterResult> LocalRegisterAsync(LocalRegisterRequest request, CancellationToken ct = default)
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
        var foundUser = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail, ct);

        if (foundUser is not null)
        {
            var identities = await _userIdentityRepository.GetByUserIdAsync(foundUser.Id, ct);
            var foundLocalIdentity = identities.FirstOrDefault(x => x.Provider == IdentityProvider.Local);

            if (foundLocalIdentity is not null)
            {
                if (foundUser.EmailVerifiedAt.HasValue)
                {
                    throw new EmailAlreadyExistedException("This email already has an account.");
                }

                var latestEmailVerificationOtp = await _userOtpRepository.GetLatestByUserIdAndPurposeAsync(
                    foundUser.Id,
                    OtpPurpose.EmailVerification,
                    ct);

                if (latestEmailVerificationOtp is not null && IsActiveOtp(latestEmailVerificationOtp, DateTime.UtcNow))
                {
                    throw new EmailVerificationPendingException(
                        "A verification code has already been sent. Please check your email.");
                }

                var refreshedAt = DateTime.UtcNow;
                var refreshedPasswordHash = _passwordHasher.Hash(request.Password);
                var refreshedOtpCode = _otpCodeGenerator.GenerateNumericCode(OtpRules.CodeLength);
                var refreshedOtpCodeHash = _passwordHasher.Hash(refreshedOtpCode);

                foundUser.UpdateProfile(displayName, foundUser.AvatarUrl, refreshedAt);
                foundLocalIdentity.UpdatePasswordHash(refreshedPasswordHash, refreshedAt);

                var refreshedEmailVerificationOtp = UserOtp.Create(
                    foundUser.Id,
                    OtpPurpose.EmailVerification,
                    refreshedOtpCodeHash,
                    OtpRules.MaxAttempts,
                    refreshedAt,
                    refreshedAt.Add(OtpRules.EmailVerificationTtl));
                refreshedEmailVerificationOtp.MarkSent(refreshedAt);

                await _localRegistrationRepository.RefreshPendingVerificationAsync(
                    foundUser,
                    foundLocalIdentity,
                    refreshedEmailVerificationOtp,
                    ct);

                await _emailJobQueue.EnqueueAsync(
                    new EmailJobMessage(
                        EmailJobType.VerifyEmail,
                        foundUser.Id,
                        email,
                        foundUser.DisplayName,
                        refreshedOtpCode,
                        DateTime.UtcNow),
                    ct);

                return new RegisterResult(
                    foundUser.Id,
                    email,
                    foundUser.DisplayName ?? string.Empty,
                    RequiresEmailVerification: true);
            }

            if (identities.Any(x => x.Provider != IdentityProvider.Local))
            {
                throw new AlternateSignInRequiredException(
                    "This email address uses another sign-in method. Continue with that provider or set a password first.");
            }

            throw new InvalidOperationException("Something is wrong with your account.");
        }

        var createdAt = DateTime.UtcNow;
        var passwordHash = _passwordHasher.Hash(request.Password);
        var otpCode = _otpCodeGenerator.GenerateNumericCode(OtpRules.CodeLength);
        var otpCodeHash = _passwordHasher.Hash(otpCode);
        var user = User.CreateLocal(email, normalizedEmail, displayName, createdAt);
        var localIdentity = UserIdentity.CreateLocal(
            user.Id,
            email,
            normalizedEmail,
            passwordHash,
            createdAt);
        var emailVerificationOtp = UserOtp.Create(
            user.Id,
            OtpPurpose.EmailVerification,
            otpCodeHash,
            OtpRules.MaxAttempts,
            createdAt,
            createdAt.Add(OtpRules.EmailVerificationTtl));
        emailVerificationOtp.MarkSent(createdAt);

        await _localRegistrationRepository.CreateAsync(user, localIdentity, emailVerificationOtp, ct);

        await _emailJobQueue.EnqueueAsync(
            new EmailJobMessage(
                EmailJobType.VerifyEmail,
                user.Id,
                email,
                user.DisplayName,
                otpCode,
                DateTime.UtcNow),
            ct);

        return new RegisterResult(
            user.Id,
            email,
            user.DisplayName ?? string.Empty,
            RequiresEmailVerification: true);
    }

    public async Task<ResendVerificationOtpResult> ResendVerificationOtpAsync(
        ResendVerificationOtpRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.", nameof(request));
        }

        var email = request.Email.Trim();
        var normalizedEmail = email.ToLowerInvariant();
        var foundUser = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail, ct);
        if (foundUser is null)
        {
            throw new EmailVerificationNotPendingException("Email verification is not pending for this account.");
        }

        if (foundUser.EmailVerifiedAt.HasValue)
        {
            throw new EmailAlreadyVerifiedException("This email address has already been verified.");
        }

        var identities = await _userIdentityRepository.GetByUserIdAsync(foundUser.Id, ct);
        if (!identities.Any(x => x.Provider == IdentityProvider.Local))
        {
            throw new EmailVerificationNotPendingException("Email verification is not pending for this account.");
        }

        var latestEmailVerificationOtp = await _userOtpRepository.GetLatestByUserIdAndPurposeAsync(
            foundUser.Id,
            OtpPurpose.EmailVerification,
            ct);

        if (latestEmailVerificationOtp is not null)
        {
            var lastSentAt = latestEmailVerificationOtp.LastSentAt ?? latestEmailVerificationOtp.CreatedAt;
            if (DateTime.UtcNow - lastSentAt < ResendCooldown)
            {
                throw new OtpResendTooSoonException("Please wait before requesting another verification code.");
            }

            if (IsActiveOtp(latestEmailVerificationOtp, DateTime.UtcNow))
            {
                latestEmailVerificationOtp.Invalidate(DateTime.UtcNow);
            }
        }

        var createdAt = DateTime.UtcNow;
        var otpCode = _otpCodeGenerator.GenerateNumericCode(OtpRules.CodeLength);
        var otpCodeHash = _passwordHasher.Hash(otpCode);
        var emailVerificationOtp = UserOtp.Create(
            foundUser.Id,
            OtpPurpose.EmailVerification,
            otpCodeHash,
            OtpRules.MaxAttempts,
            createdAt,
            createdAt.Add(OtpRules.EmailVerificationTtl));
        emailVerificationOtp.MarkSent(createdAt);

        await _userOtpRepository.RefreshAsync(latestEmailVerificationOtp, emailVerificationOtp, ct);

        await _emailJobQueue.EnqueueAsync(
            new EmailJobMessage(
                EmailJobType.VerifyEmail,
                foundUser.Id,
                email,
                foundUser.DisplayName,
                otpCode,
                DateTime.UtcNow),
            ct);

        return new ResendVerificationOtpResult(email, RequiresEmailVerification: true);
    }

    public async Task<VerifyEmailOtpResult> VerifyEmailOtpAsync(VerifyEmailOtpRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OtpCode))
        {
            throw new ArgumentException("Otp code is required.", nameof(request));
        }

        var email = request.Email.Trim();
        var normalizedEmail = email.ToLowerInvariant();
        var foundUser = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail, ct);
        if (foundUser is null)
        {
            throw new EmailVerificationNotPendingException("Email verification is not pending for this account.");
        }

        if (foundUser.EmailVerifiedAt.HasValue)
        {
            throw new EmailAlreadyVerifiedException("This email address has already been verified.");
        }

        var identities = await _userIdentityRepository.GetByUserIdAsync(foundUser.Id, ct);
        if (!identities.Any(x => x.Provider == IdentityProvider.Local))
        {
            throw new EmailVerificationNotPendingException("Email verification is not pending for this account.");
        }

        var latestEmailVerificationOtp = await _userOtpRepository.GetLatestByUserIdAndPurposeAsync(
            foundUser.Id,
            OtpPurpose.EmailVerification,
            ct);

        if (latestEmailVerificationOtp is null)
        {
            throw new VerificationOtpInactiveException("Verification code is no longer valid. Request a new one.");
        }

        if (latestEmailVerificationOtp.IsUsed()
            || latestEmailVerificationOtp.IsInvalidated()
            || latestEmailVerificationOtp.IsExpired(DateTime.UtcNow))
        {
            throw new VerificationOtpInactiveException("Verification code is no longer valid. Request a new one.");
        }

        if (_passwordHasher.Verify(request.OtpCode, latestEmailVerificationOtp.CodeHash))
        {
            latestEmailVerificationOtp.MarkUsed(DateTime.UtcNow);
            foundUser.VerifyEmail(DateTime.UtcNow);

            await _userOtpRepository.CompleteEmailVerificationAsync(foundUser, latestEmailVerificationOtp, ct);

            return new VerifyEmailOtpResult(foundUser.Id, email, EmailVerified: true);
        }

        latestEmailVerificationOtp.IncrementAttempt();
        await _userOtpRepository.IncrementAttemptAsync(latestEmailVerificationOtp, ct);

        if (latestEmailVerificationOtp.HasExceededAttempts())
        {
            throw new OtpMaxAttemptsExceededException(
                "This verification code is no longer valid. Request a new one.");
        }

        throw new WrongOtpException("Incorrect verification code.");
    }

    private static bool IsActiveOtp(UserOtp otp, DateTime at)
    {
        return !otp.IsUsed()
            && !otp.IsInvalidated()
            && !otp.IsExpired(at)
            && !otp.HasExceededAttempts();
    }
}
