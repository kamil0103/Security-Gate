using System.Security.Cryptography;
using System.Text;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Application.Identity;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IDeviceIdentityService _deviceIdentityService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtOptions _jwtOptions;

    public AuthenticationService(
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        ITokenRepository tokenRepository,
        IDeviceIdentityService deviceIdentityService,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        JwtOptions jwtOptions)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _tokenRepository = tokenRepository;
        _deviceIdentityService = deviceIdentityService;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _jwtOptions = jwtOptions;
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, DeviceEnrollmentRequest? deviceRequest = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        ValidatePasswordStrength(request.Password);

        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken).ConfigureAwait(false))
        {
            throw new AuthenticationException("Username is already taken.");
        }

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken).ConfigureAwait(false))
        {
            throw new AuthenticationException("Email is already registered.");
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(request.Password)
        };

        await _userRepository.AddAsync(user, cancellationToken).ConfigureAwait(false);

        if (_emailService.IsConfigured)
        {
            var token = _tokenService.GenerateEmailVerificationToken();
            var tokenHash = HashToken(token);
            var verificationToken = new EmailVerificationToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
            };

            await _tokenRepository.AddEmailVerificationTokenAsync(verificationToken, cancellationToken).ConfigureAwait(false);
            await SendVerificationEmailAsync(user.Email, token, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // If email is not configured, auto-verify for development convenience.
            user.EmailVerified = true;
            user.Status = UserStatus.Active;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await CreateLoginResponseAsync(user, deviceRequest, ipAddress, userAgent, cancellationToken).ConfigureAwait(false);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, DeviceEnrollmentRequest? deviceRequest = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail, cancellationToken).ConfigureAwait(false)
            ?? throw new AuthenticationException("Invalid credentials.");

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException("Invalid credentials.");
        }

        if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Disabled)
        {
            throw new AuthenticationException("Account is not active.");
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await CreateLoginResponseAsync(user, deviceRequest, ipAddress, userAgent, cancellationToken).ConfigureAwait(false);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(refreshToken);
        var session = await _sessionRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken).ConfigureAwait(false);

        if (session is not null && !session.IsRevoked && !session.IsExpired)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;
            await _sessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<TokenPair> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var session = await _sessionRepository.GetByRefreshTokenHashAsync(tokenHash, cancellationToken).ConfigureAwait(false)
            ?? throw new AuthenticationException("Invalid refresh token.");

        if (session.IsRevoked || session.IsExpired)
        {
            throw new AuthenticationException("Refresh token is no longer valid.");
        }

        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken).ConfigureAwait(false)
            ?? throw new AuthenticationException("User not found.");

        if (user.Status == UserStatus.Suspended || user.Status == UserStatus.Disabled)
        {
            throw new AuthenticationException("Account is not active.");
        }

        // Rotate refresh token: revoke old, issue new.
        session.RevokedAt = DateTimeOffset.UtcNow;
        await _sessionRepository.UpdateAsync(session, cancellationToken).ConfigureAwait(false);

        return await GenerateAndStoreTokensAsync(user, session.IpAddress, session.UserAgent, cancellationToken).ConfigureAwait(false);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false)
            ?? throw new AuthenticationException("User not found.");

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new AuthenticationException("Current password is incorrect.");
        }

        ValidatePasswordStrength(request.NewPassword);
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await _sessionRepository.RevokeAllUserSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false);

        if (user is null)
        {
            // Do not reveal whether the email exists.
            return;
        }

        var token = _tokenService.GeneratePasswordResetToken();
        var tokenHash = HashToken(token);
        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        await _tokenRepository.AddPasswordResetTokenAsync(resetToken, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_emailService.IsConfigured)
        {
            await SendPasswordResetEmailAsync(user.Email, token, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false)
            ?? throw new AuthenticationException("Invalid request.");

        var tokenHash = HashToken(request.Token);
        var resetToken = await _tokenRepository.GetPasswordResetTokenAsync(tokenHash, cancellationToken).ConfigureAwait(false)
            ?? throw new AuthenticationException("Invalid request.");

        if (resetToken.UserId != user.Id || resetToken.IsUsed || resetToken.IsExpired)
        {
            throw new AuthenticationException("Invalid or expired token.");
        }

        ValidatePasswordStrength(request.NewPassword);
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        resetToken.UsedAt = DateTimeOffset.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await _tokenRepository.UpdatePasswordResetTokenAsync(resetToken, cancellationToken).ConfigureAwait(false);
        await _sessionRepository.RevokeAllUserSessionsAsync(user.Id, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken).ConfigureAwait(false)
            ?? throw new AuthenticationException("Invalid request.");

        var tokenHash = HashToken(request.Token);
        var verificationToken = await _tokenRepository.GetEmailVerificationTokenAsync(tokenHash, cancellationToken).ConfigureAwait(false)
            ?? throw new AuthenticationException("Invalid request.");

        if (verificationToken.UserId != user.Id || verificationToken.IsUsed || verificationToken.IsExpired)
        {
            throw new AuthenticationException("Invalid or expired token.");
        }

        user.EmailVerified = true;
        user.Status = UserStatus.Active;
        verificationToken.UsedAt = DateTimeOffset.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await _tokenRepository.UpdateEmailVerificationTokenAsync(verificationToken, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return user is null ? null : MapToDto(user);
    }

    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _sessionRepository.RevokeAllUserSessionsAsync(userId, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<LoginResponse> CreateLoginResponseAsync(User user, DeviceEnrollmentRequest? deviceRequest, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var tokens = await GenerateAndStoreTokensAsync(user, ipAddress, userAgent, cancellationToken).ConfigureAwait(false);
        var deviceEnrollment = deviceRequest ?? CreateFallbackDeviceRequest(userAgent);
        var deviceResult = await _deviceIdentityService.RecognizeOrEnrollAsync(user.Id, deviceEnrollment, ipAddress ?? "unknown", cancellationToken).ConfigureAwait(false);

        return new LoginResponse
        {
            User = MapToDto(user),
            Tokens = tokens,
            Device = deviceResult
        };
    }

    private async Task<TokenPair> GenerateAndStoreTokensAsync(User user, string? ipAddress, string? userAgent, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        var session = new Session
        {
            UserId = user.Id,
            RefreshTokenHash = refreshTokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        await _sessionRepository.AddAsync(session, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new TokenPair
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            RefreshTokenExpiresAt = session.ExpiresAt
        };
    }

    private static DeviceEnrollmentRequest CreateFallbackDeviceRequest(string? userAgent)
    {
        var fallbackFingerprint = string.IsNullOrWhiteSpace(userAgent)
            ? $"fallback-{Guid.NewGuid()}"
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userAgent)));

        return new DeviceEnrollmentRequest
        {
            DeviceId = Guid.NewGuid().ToString(),
            Name = "Unknown Device",
            Fingerprint = fallbackFingerprint,
            UserAgent = userAgent
        };
    }

    private async Task SendVerificationEmailAsync(string email, string token, CancellationToken cancellationToken)
    {
        var subject = "Verify your Security Gateway account";
        var body = $"Your verification token is: {token}";
        await _emailService.SendEmailAsync(email, subject, body, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendPasswordResetEmailAsync(string email, string token, CancellationToken cancellationToken)
    {
        var subject = "Reset your Security Gateway password";
        var body = $"Your password reset token is: {token}";
        await _emailService.SendEmailAsync(email, subject, body, cancellationToken).ConfigureAwait(false);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            Status = user.Status,
            EmailVerified = user.EmailVerified,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static void ValidatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            throw new AuthenticationException("Password must be at least 12 characters long.");
        }
    }
}
