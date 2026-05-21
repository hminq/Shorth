using Application.Features.Auth.Configurations;
using Application.Features.Auth.Dtos;
using Application.Features.Auth.Interfaces;
using Application.Features.Auth.Messages;
using Application.Features.Auth.Utilities;
using System.Security.Cryptography;
using Domain.Features.Auth.Constants;
using Domain.Features.Auth.Entities;
using Domain.Features.Auth.Enums;
using Domain.Features.Auth.Exceptions;

namespace Application.Features.Auth.Services;

public sealed class AuthService
{
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan OtpRequestRateLimitWindow = TimeSpan.FromHours(1);
    private const int OtpRequestRateLimitMaxRequests = 5;

    private readonly IUserRepository _userRepository;
    private readonly IUserIdentityRepository _userIdentityRepository;
    private readonly IUserOtpRepository _userOtpRepository;
    private readonly IUserRefreshTokenRepository _userRefreshTokenRepository;
    private readonly ILocalRegistrationRepository _localRegistrationRepository;
    private readonly IExternalIdentityRepository _externalIdentityRepository;
    private readonly IEmailJobQueue _emailJobQueue;
    private readonly IOtpCodeGenerator _otpCodeGenerator;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IGoogleAuthProvider _googleAuthProvider;
    private readonly IGoogleAuthStateRepository _googleAuthStateStore;
    private readonly IAuthLinkBuilder _authLinkBuilder;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IOtpRequestRateLimiter _otpRequestRateLimiter;
    private readonly RefreshTokenOptions _refreshTokenOptions;

    public AuthService(
        IUserRepository userRepository,
        IUserIdentityRepository userIdentityRepository,
        IUserOtpRepository userOtpRepository,
        IUserRefreshTokenRepository userRefreshTokenRepository,
        ILocalRegistrationRepository localRegistrationRepository,
        IExternalIdentityRepository externalIdentityRepository,
        IEmailJobQueue emailJobQueue,
        IOtpCodeGenerator otpCodeGenerator,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IRefreshTokenHasher refreshTokenHasher,
        IPasswordHasher passwordHasher,
        IGoogleAuthProvider googleAuthProvider,
        IGoogleAuthStateRepository googleAuthStateStore,
        IAuthLinkBuilder authLinkBuilder,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IOtpRequestRateLimiter otpRequestRateLimiter,
        RefreshTokenOptions refreshTokenOptions)
    {
        _userRepository = userRepository;
        _userIdentityRepository = userIdentityRepository;
        _userOtpRepository = userOtpRepository;
        _userRefreshTokenRepository = userRefreshTokenRepository;
        _localRegistrationRepository = localRegistrationRepository;
        _externalIdentityRepository = externalIdentityRepository;
        _emailJobQueue = emailJobQueue;
        _otpCodeGenerator = otpCodeGenerator;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _refreshTokenHasher = refreshTokenHasher;
        _passwordHasher = passwordHasher;
        _googleAuthProvider = googleAuthProvider;
        _googleAuthStateStore = googleAuthStateStore;
        _authLinkBuilder = authLinkBuilder;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _otpRequestRateLimiter = otpRequestRateLimiter;
        _refreshTokenOptions = refreshTokenOptions;
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

    public async Task<LoginResult> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ArgumentException("Authorization code is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            throw new InvalidGoogleAuthStateException("Google auth state is missing.");
        }

        if (await _googleAuthStateStore.TakeAsync(request.State, ct) is null)
        {
            throw new InvalidGoogleAuthStateException("Google auth state is invalid or expired.");
        }

        var googleUserInfo = await _googleAuthProvider.GetUserInfoAsync(request, ct);
        if (string.IsNullOrWhiteSpace(googleUserInfo.Email) || !googleUserInfo.IsEmailVerified)
        {
            throw new GoogleEmailUnavailableException("Google account must provide a verified email address.");
        }

        var email = googleUserInfo.Email.Trim();
        var normalizedEmail = EmailNormalizer.Normalize(email);
        var now = DateTime.UtcNow;

        var googleIdentity = await _userIdentityRepository.GetByProviderAndProviderUserIdAsync(
            IdentityProvider.Google,
            googleUserInfo.ProviderUserId,
            ct);

        if (googleIdentity is not null)
        {
            var user = await _userRepository.GetByIdAsync(googleIdentity.UserId, ct);
            if (user is null)
            {
                throw new InvalidOperationException("Something is wrong with your account.");
            }

            return await SignInGoogleUserAsync(user, now, ct);
        }

        var existingUser = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail, ct);
        if (existingUser is not null)
        {
            if (existingUser.Status == UserStatus.Disabled)
            {
                throw new AccountDisableException("This account is disabled.");
            }

            var linkedGoogleIdentity = UserIdentity.CreateExternal(
                existingUser.Id,
                IdentityProvider.Google,
                googleUserInfo.ProviderUserId,
                email,
                now);

            if (!existingUser.EmailVerifiedAt.HasValue)
            {
                existingUser.VerifyEmail(now);
            }

            existingUser.UpdateProfile(
                googleUserInfo.DisplayName ?? existingUser.DisplayName,
                existingUser.AvatarUrl,
                now);
            existingUser.MarkLastLogin(now);

            await _externalIdentityRepository.LinkAsync(existingUser, linkedGoogleIdentity, ct);

            return await CreateLoginResultAsync(existingUser, now, ct);
        }

        var newUser = User.CreateOAuth(
            IdentityProvider.Google,
            email,
            normalizedEmail,
            googleUserInfo.DisplayName,
            avatarUrl: null,
            now);
        newUser.VerifyEmail(now);
        newUser.MarkLastLogin(now);

        var newGoogleIdentity = UserIdentity.CreateExternal(
            newUser.Id,
            IdentityProvider.Google,
            googleUserInfo.ProviderUserId,
            email,
            now);

        await _externalIdentityRepository.CreateAsync(newUser, newGoogleIdentity, ct);

        return await CreateLoginResultAsync(newUser, now, ct);
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

        var emailNormalized = EmailNormalizer.Normalize(request.Email);
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
                    "This email address uses a different sign-in method. Continue with that method or reset/set a password.");
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

        return await CreateLoginResultAsync(foundUser, DateTime.UtcNow, ct);
    }

    public async Task<LoginResult> RefreshAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token is missing.");
        }

        var now = DateTime.UtcNow;
        var tokenHash = _refreshTokenHasher.Hash(refreshToken);
        var foundRefreshToken = await _userRefreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);
        if (foundRefreshToken is null)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid.");
        }

        if (foundRefreshToken.IsRevoked())
        {
            await _userRefreshTokenRepository.RevokeActiveByUserIdAsync(foundRefreshToken.UserId, now, ct);
            throw new UnauthorizedAccessException("Refresh token has been reused.");
        }

        if (foundRefreshToken.IsExpired(now))
        {
            throw new UnauthorizedAccessException("Refresh token is expired.");
        }

        var foundUser = await _userRepository.GetByIdAsync(foundRefreshToken.UserId, ct);
        if (foundUser is null)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid.");
        }

        if (foundUser.Status == UserStatus.Disabled)
        {
            await _userRefreshTokenRepository.RevokeActiveByUserIdAsync(foundUser.Id, now, ct);
            throw new AccountDisableException("This account is disabled.");
        }

        var replacementRefreshToken = CreateRefreshToken(foundUser.Id, now);
        foundRefreshToken.Replace(replacementRefreshToken.Token.Id, now);

        await _userRefreshTokenRepository.RotateAsync(foundRefreshToken, replacementRefreshToken.Token, ct);

        return CreateLoginResult(foundUser, replacementRefreshToken.PlainText);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = _refreshTokenHasher.Hash(refreshToken);
        var foundRefreshToken = await _userRefreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);
        if (foundRefreshToken is null || foundRefreshToken.IsRevoked())
        {
            return;
        }

        foundRefreshToken.Revoke(DateTime.UtcNow);
        await _userRefreshTokenRepository.RevokeAsync(foundRefreshToken, ct);
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

        var normalizedEmail = EmailNormalizer.Normalize(request.Email);
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

                var refreshedEmailJob = new EmailJobMessage(
                    EmailJobType.VerifyEmail,
                    foundUser.Id,
                    email,
                    foundUser.DisplayName,
                    refreshedOtpCode,
                    DateTime.UtcNow);

                await _localRegistrationRepository.RefreshPendingVerificationAsync(
                    foundUser,
                    foundLocalIdentity,
                    refreshedEmailVerificationOtp,
                    refreshedEmailJob,
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
                    "This email address uses a different sign-in method. Continue with that method or reset/set a password.");
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

        var emailJob = new EmailJobMessage(
            EmailJobType.VerifyEmail,
            user.Id,
            email,
            user.DisplayName,
            otpCode,
            DateTime.UtcNow);

        await _localRegistrationRepository.CreateAsync(user, localIdentity, emailVerificationOtp, emailJob, ct);

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
        var normalizedEmail = EmailNormalizer.Normalize(email);

        if (!await CanSendOtpAsync("register-verification", normalizedEmail, request.ClientIp, ct))
        {
            throw new OtpResendTooSoonException("Please wait before requesting another code.");
        }

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
        var normalizedEmail = EmailNormalizer.Normalize(email);

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

    public async Task<ForgotPasswordResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.", nameof(request));
        }

        var email = request.Email.Trim();
        var normalizedEmail = EmailNormalizer.Normalize(email);

        if (!await CanSendOtpAsync("forgot-password", normalizedEmail, request.ClientIp, ct))
        {
            return new ForgotPasswordResult(email);
        }

        var foundUser = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail, ct);

        if (foundUser is null
            || foundUser.Status == UserStatus.Disabled
            || !foundUser.EmailVerifiedAt.HasValue
            || string.IsNullOrWhiteSpace(foundUser.Email)
            || string.IsNullOrWhiteSpace(foundUser.EmailNormalized))
        {
            return new ForgotPasswordResult(email);
        }

        var latestPasswordResetOtp = await _userOtpRepository.GetLatestByUserIdAndPurposeAsync(
            foundUser.Id,
            OtpPurpose.PasswordReset,
            ct);

        if (latestPasswordResetOtp is not null)
        {
            var lastSentAt = latestPasswordResetOtp.LastSentAt ?? latestPasswordResetOtp.CreatedAt;
            if (DateTime.UtcNow - lastSentAt < ResendCooldown)
            {
                return new ForgotPasswordResult(email);
            }

            if (IsActiveOtp(latestPasswordResetOtp, DateTime.UtcNow))
            {
                latestPasswordResetOtp.Invalidate(DateTime.UtcNow);
            }
        }

        var createdAt = DateTime.UtcNow;
        var otpCode = _otpCodeGenerator.GenerateNumericCode(OtpRules.CodeLength);
        var otpCodeHash = _passwordHasher.Hash(otpCode);
        var passwordResetOtp = UserOtp.Create(
            foundUser.Id,
            OtpPurpose.PasswordReset,
            otpCodeHash,
            OtpRules.MaxAttempts,
            createdAt,
            createdAt.Add(OtpRules.PasswordResetTtl));
        passwordResetOtp.MarkSent(createdAt);

        var emailJob = new EmailJobMessage(
            EmailJobType.ForgotPassword,
            foundUser.Id,
            foundUser.Email,
            foundUser.DisplayName,
            otpCode,
            DateTime.UtcNow,
            _authLinkBuilder.BuildPasswordResetUrl(foundUser.Email));

        await _userOtpRepository.RefreshWithEmailJobAsync(latestPasswordResetOtp, passwordResetOtp, emailJob, ct);

        return new ForgotPasswordResult(email);
    }

    private async Task<bool> CanSendOtpAsync(
        string purpose,
        string normalizedEmail,
        string? clientIp,
        CancellationToken ct)
    {
        var normalizedClientIp = string.IsNullOrWhiteSpace(clientIp) ? "unknown" : clientIp.Trim();
        var emailKey = $"rate:otp-send:{purpose}:email:{normalizedEmail}";
        var ipKey = $"rate:otp-send:{purpose}:ip:{normalizedClientIp}";

        var emailAllowed = await _otpRequestRateLimiter.TryConsumeAsync(
            emailKey,
            OtpRequestRateLimitMaxRequests,
            OtpRequestRateLimitWindow,
            ct);
        var ipAllowed = await _otpRequestRateLimiter.TryConsumeAsync(
            ipKey,
            OtpRequestRateLimitMaxRequests,
            OtpRequestRateLimitWindow,
            ct);

        return emailAllowed && ipAllowed;
    }

    public async Task<VerifyPasswordResetResult> VerifyPasswordResetAsync(
        VerifyPasswordResetRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.OtpCode))
        {
            throw new ArgumentException("Reset code is required.", nameof(request));
        }

        var email = request.Email.Trim();
        var normalizedEmail = EmailNormalizer.Normalize(email);
        var foundUser = await _userRepository.GetByEmailNormalizedAsync(normalizedEmail, ct);
        if (foundUser is null
            || foundUser.Status == UserStatus.Disabled
            || !foundUser.EmailVerifiedAt.HasValue
            || string.IsNullOrWhiteSpace(foundUser.Email)
            || string.IsNullOrWhiteSpace(foundUser.EmailNormalized))
        {
            throw new VerificationOtpInactiveException("Reset code is no longer valid. Request a new one.");
        }

        var latestPasswordResetOtp = await _userOtpRepository.GetLatestByUserIdAndPurposeAsync(
            foundUser.Id,
            OtpPurpose.PasswordReset,
            ct);

        if (latestPasswordResetOtp is null)
        {
            throw new VerificationOtpInactiveException("Reset code is no longer valid. Request a new one.");
        }

        if (latestPasswordResetOtp.IsUsed()
            || latestPasswordResetOtp.IsInvalidated()
            || latestPasswordResetOtp.IsExpired(DateTime.UtcNow))
        {
            throw new VerificationOtpInactiveException("Reset code is no longer valid. Request a new one.");
        }

        if (!_passwordHasher.Verify(request.OtpCode, latestPasswordResetOtp.CodeHash))
        {
            latestPasswordResetOtp.IncrementAttempt();
            await _userOtpRepository.IncrementAttemptAsync(latestPasswordResetOtp, ct);

            if (latestPasswordResetOtp.HasExceededAttempts())
            {
                throw new OtpMaxAttemptsExceededException("Too many attempts. Request a new reset code.");
            }

            throw new WrongOtpException("Incorrect reset code.");
        }

        var now = DateTime.UtcNow;
        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        latestPasswordResetOtp.MarkUsed(now);
        await _userOtpRepository.CompletePasswordResetVerificationAsync(latestPasswordResetOtp, ct);
        await _passwordResetTokenRepository.StoreAsync(
            resetToken,
            new PasswordResetTokenState(foundUser.Id, email, now),
            ct);

        return new VerifyPasswordResetResult(email, resetToken);
    }

    public async Task<PasswordResetResult> CompletePasswordResetAsync(
        CompletePasswordResetRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ResetToken))
        {
            throw new VerificationOtpInactiveException("Reset session is no longer valid. Request a new code.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("Password is required.", nameof(request));
        }

        var resetState = await _passwordResetTokenRepository.TakeAsync(request.ResetToken, ct);
        if (resetState is null)
        {
            throw new VerificationOtpInactiveException("Reset session is no longer valid. Request a new code.");
        }

        var foundUser = await _userRepository.GetByIdAsync(resetState.UserId, ct);
        if (foundUser is null
            || foundUser.Status == UserStatus.Disabled
            || !foundUser.EmailVerifiedAt.HasValue
            || string.IsNullOrWhiteSpace(foundUser.Email)
            || string.IsNullOrWhiteSpace(foundUser.EmailNormalized))
        {
            throw new VerificationOtpInactiveException("Reset session is no longer valid. Request a new code.");
        }

        var identities = await _userIdentityRepository.GetByUserIdAsync(foundUser.Id, ct);
        var localIdentity = identities.FirstOrDefault(x => x.Provider == IdentityProvider.Local);
        var shouldAddLocalIdentity = localIdentity is null;
        var passwordHash = _passwordHasher.Hash(request.NewPassword);
        var now = DateTime.UtcNow;

        if (localIdentity is null)
        {
            localIdentity = UserIdentity.CreateLocal(
                foundUser.Id,
                foundUser.Email,
                foundUser.EmailNormalized,
                passwordHash,
                now);
        }
        else
        {
            localIdentity.UpdatePasswordHash(passwordHash, now);
        }

        await _userRepository.CompletePasswordResetAsync(
            localIdentity,
            shouldAddLocalIdentity,
            ct);

        return new PasswordResetResult(foundUser.Email, PasswordReset: true);
    }

    private static bool IsActiveOtp(UserOtp otp, DateTime at)
    {
        return !otp.IsUsed()
            && !otp.IsInvalidated()
            && !otp.IsExpired(at)
            && !otp.HasExceededAttempts();
    }

    private async Task<LoginResult> SignInGoogleUserAsync(User user, DateTime signedInAt, CancellationToken ct)
    {
        if (user.Status == UserStatus.Disabled)
        {
            throw new AccountDisableException("This account is disabled.");
        }

        user.MarkLastLogin(signedInAt);
        await _userRepository.UpdateAsync(user, ct);

        return await CreateLoginResultAsync(user, signedInAt, ct);
    }

    private async Task<LoginResult> CreateLoginResultAsync(User user, DateTime issuedAt, CancellationToken ct)
    {
        var refreshToken = CreateRefreshToken(user.Id, issuedAt);
        await _userRefreshTokenRepository.AddAsync(refreshToken.Token, ct);

        return CreateLoginResult(user, refreshToken.PlainText);
    }

    private LoginResult CreateLoginResult(User user, string refreshToken)
    {
        return new LoginResult(
            _jwtTokenGenerator.GenerateToken(user),
            refreshToken,
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName ?? string.Empty);
    }

    private RefreshTokenIssue CreateRefreshToken(Guid userId, DateTime issuedAt)
    {
        var plainTextToken = _refreshTokenGenerator.GenerateToken();
        var tokenHash = _refreshTokenHasher.Hash(plainTextToken);
        var token = UserRefreshToken.Create(
            userId,
            tokenHash,
            issuedAt,
            issuedAt.AddDays(_refreshTokenOptions.TtlDays));

        return new RefreshTokenIssue(plainTextToken, token);
    }

    private sealed record RefreshTokenIssue(string PlainText, UserRefreshToken Token);
}
