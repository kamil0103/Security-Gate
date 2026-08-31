using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Identity;
using SecurityGateway.Application.Identity.DTOs;
using SecurityGateway.Application.ThreatDetection;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Infrastructure.AccessControl.Repositories;
using SecurityGateway.Infrastructure.AccessControl.Services;
using SecurityGateway.Infrastructure.Identity;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.Persistence.Repositories;
using SecurityGateway.Tests.Helpers;
using SecurityGateway.Tests.TestHelpers;
using Xunit;

namespace SecurityGateway.Tests.Identity;

public class AuthenticationServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuthenticationService _service;
    private readonly FakeEmailService _emailService;

    public AuthenticationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        var userRepository = new UserRepository(_context);
        var sessionRepository = new SessionRepository(_context);
        var tokenRepository = new TokenRepository(_context);
        var deviceRepository = new DeviceRepository(_context);
        var passwordHasher = new Argon2PasswordHasher();
        var jwtOptions = new JwtOptions
        {
            Secret = "ThisIsATestSecretKeyThatIsAtLeast32CharsLong!",
            Issuer = "SecurityGateway",
            Audience = "SecurityGateway",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };
        var tokenService = new JwtTokenService(jwtOptions);
        _emailService = new FakeEmailService();
        var deviceIdentityService = new DeviceIdentityService(deviceRepository, _context);
        var trustedNetworkRepository = new TrustedNetworkRepository(_context);
        var blocklistRepository = new BlocklistRepository(_context);
        var accessDecisionRepository = new AccessDecisionRepository(_context);
        var accessControlService = new AccessControlService(
            trustedNetworkRepository,
            blocklistRepository,
            accessDecisionRepository,
            deviceRepository,
            new FakeThreatDetectionService(),
            new FakeAuditService(),
            _context);

        _service = new AuthenticationService(
            userRepository,
            sessionRepository,
            tokenRepository,
            deviceIdentityService,
            accessControlService,
            new FakeThreatDetectionService(),
            passwordHasher,
            tokenService,
            _emailService,
            new FakeAuditService(),
            _context,
            jwtOptions);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUser()
    {
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "StrongPassword123!"
        };

        var result = await _service.RegisterAsync(request);

        Assert.Equal("testuser", result.User.Username);
        Assert.Equal("test@example.com", result.User.Email);
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens.RefreshToken));
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ThrowsException()
    {
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "first@example.com",
            Password = "StrongPassword123!"
        };

        await _service.RegisterAsync(request);

        var duplicate = request with { Email = "second@example.com" };

        var exception = await Assert.ThrowsAsync<AuthenticationException>(() => _service.RegisterAsync(duplicate));
        Assert.Contains("Username", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
    {
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "StrongPassword123!"
        };

        await _service.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "testuser",
            Password = "StrongPassword123!"
        };

        var result = await _service.LoginAsync(loginRequest);

        Assert.Equal("testuser", result.User.Username);
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens.AccessToken));
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsException()
    {
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "StrongPassword123!"
        };

        await _service.RegisterAsync(registerRequest);

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "testuser",
            Password = "WrongPassword123!"
        };

        await Assert.ThrowsAsync<AuthenticationException>(() => _service.LoginAsync(loginRequest));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectPassword_ChangesPassword()
    {
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "StrongPassword123!"
        };

        var registerResult = await _service.RegisterAsync(registerRequest);

        var changeRequest = new ChangePasswordRequest
        {
            CurrentPassword = "StrongPassword123!",
            NewPassword = "NewStrongPassword123!"
        };

        await _service.ChangePasswordAsync(registerResult.User.Id, changeRequest);

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "testuser",
            Password = "NewStrongPassword123!"
        };

        var result = await _service.LoginAsync(loginRequest);
        Assert.Equal("testuser", result.User.Username);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private sealed class FakeEmailService : IEmailService
    {
        public bool IsConfigured => false;

        public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
