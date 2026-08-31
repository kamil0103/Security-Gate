using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.WebAuthn;
using SecurityGateway.Domain.WebAuthn;
using SecurityGateway.Infrastructure.Persistence;
using SecurityGateway.Infrastructure.WebAuthn.Repositories;
using SecurityGateway.Infrastructure.WebAuthn.Services;
using Xunit;

namespace SecurityGateway.Tests.AdvancedSecurity;

public class WebAuthnServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly WebAuthnService _service;

    public WebAuthnServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _service = new WebAuthnService(
            new WebAuthnCredentialRepository(_context),
            new WebAuthnOptions { RelyingPartyId = "localhost", RelyingPartyName = "Test" },
            _context);
    }

    [Fact]
    public async Task BeginRegistrationAsync_ReturnsChallenge()
    {
        var userId = Guid.NewGuid();
        var result = await _service.BeginRegistrationAsync(userId, "testuser");

        Assert.Equal(userId, result.UserId);
        Assert.Equal("testuser", result.Username);
        Assert.NotNull(result.Challenge);
        Assert.Equal("localhost", result.RelyingPartyId);
    }

    [Fact]
    public async Task CompleteRegistrationAsync_PersistsCredential()
    {
        var userId = Guid.NewGuid();
        await _service.CompleteRegistrationAsync(userId, new CompleteRegistrationRequest
        {
            CredentialId = "cred-1",
            PublicKey = "key",
            CredentialType = "public-key",
            DeviceName = "YubiKey"
        });

        var credentials = await _service.GetCredentialsAsync(userId);

        Assert.Single(credentials);
        Assert.Equal("cred-1", credentials[0].CredentialId);
        Assert.Equal("YubiKey", credentials[0].DeviceName);
    }

    [Fact]
    public async Task DeleteCredentialAsync_RemovesCredential()
    {
        var userId = Guid.NewGuid();
        await _service.CompleteRegistrationAsync(userId, new CompleteRegistrationRequest
        {
            CredentialId = "cred-2",
            PublicKey = "key",
            CredentialType = "public-key"
        });

        await _service.DeleteCredentialAsync(userId, "cred-2");

        var credentials = await _service.GetCredentialsAsync(userId);
        Assert.Empty(credentials);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
