using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Identity;
using SecurityGateway.Domain.Identity;

namespace SecurityGateway.Infrastructure.Persistence;

public sealed class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly DefaultAdminOptions _options;

    public DataSeeder(ApplicationDbContext context, IPasswordHasher passwordHasher, DefaultAdminOptions options)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _options = options;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        if (!await _context.Users.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var admin = new User
            {
                Username = _options.Username,
                Email = _options.Email,
                PasswordHash = _passwordHasher.HashPassword(_options.Password),
                Role = UserRole.Administrator,
                Status = UserStatus.Active,
                EmailVerified = true
            };

            await _context.Users.AddAsync(admin, cancellationToken).ConfigureAwait(false);
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
