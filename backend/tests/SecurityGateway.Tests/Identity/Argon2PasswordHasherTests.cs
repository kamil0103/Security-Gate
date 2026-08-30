using SecurityGateway.Infrastructure.Identity;
using Xunit;

namespace SecurityGateway.Tests.Identity;

public class Argon2PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ReturnsNonEmptyHash()
    {
        var hash = _hasher.HashPassword("StrongPassword123!");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.StartsWith("$argon2id$", hash);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        var password = "StrongPassword123!";
        var hash = _hasher.HashPassword(password);

        var result = _hasher.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = _hasher.HashPassword("StrongPassword123!");

        var result = _hasher.VerifyPassword("WrongPassword123!", hash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithInvalidHash_ReturnsFalse()
    {
        var result = _hasher.VerifyPassword("StrongPassword123!", "not-a-valid-hash");

        Assert.False(result);
    }
}
