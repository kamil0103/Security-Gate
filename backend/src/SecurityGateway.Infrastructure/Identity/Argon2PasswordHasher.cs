using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using SecurityGateway.Application.Identity;

namespace SecurityGateway.Infrastructure.Identity;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    // Parameters chosen for a balance of security and performance.
    // Tuned for modern hardware; consider increasing memory/iterations in high-security environments.
    private const int SaltLength = 16;
    private const int HashLength = 32;
    private const int DegreeOfParallelism = 4;
    private const int MemorySizeKb = 65536; // 64 MB
    private const int Iterations = 3;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var hash = Hash(password, salt);

        // Format: $argon2id$v=19$m=65536,t=3,p=4$base64salt$base64hash
        return $"$argon2id$v=19$m={MemorySizeKb},t={Iterations},p={DegreeOfParallelism}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        var parts = hash.Split('$');
        if (parts.Length != 6 || parts[1] != "argon2id")
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[4]);
        var expectedHash = Convert.FromBase64String(parts[5]);
        var actualHash = Hash(password, salt);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private byte[] Hash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySizeKb,
            Iterations = Iterations
        };

        return argon2.GetBytes(HashLength);
    }
}
