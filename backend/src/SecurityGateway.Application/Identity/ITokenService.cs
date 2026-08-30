using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Application.Identity;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string GenerateEmailVerificationToken();
    string GeneratePasswordResetToken();
}
