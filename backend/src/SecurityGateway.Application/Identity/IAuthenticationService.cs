using SecurityGateway.Application.Identity.DTOs;

namespace SecurityGateway.Application.Identity;

public interface IAuthenticationService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request, DeviceEnrollmentRequest? deviceRequest = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, DeviceEnrollmentRequest? deviceRequest = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<TokenPair> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
