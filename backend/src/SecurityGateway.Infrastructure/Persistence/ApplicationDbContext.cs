using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Identity;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Domain.IpIntelligence;

namespace SecurityGateway.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceIpAddress> DeviceIpAddresses => Set<DeviceIpAddress>();
    public DbSet<IpAddress> IpAddresses => Set<IpAddress>();
    public DbSet<IpUserAssociation> IpUserAssociations => Set<IpUserAssociation>();
    public DbSet<IpDeviceAssociation> IpDeviceAssociations => Set<IpDeviceAssociation>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.Role).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RefreshTokenHash).IsUnique();
            entity.HasOne(e => e.User).WithMany(u => u.Sessions).HasForeignKey(e => e.UserId);
            entity.Property(e => e.RefreshTokenHash).HasMaxLength(128);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Fingerprint });
            entity.HasOne(e => e.User).WithMany(u => u.Devices).HasForeignKey(e => e.UserId);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Fingerprint).HasMaxLength(256);
            entity.Property(e => e.PublicKey).HasMaxLength(512);
            entity.Property(e => e.CredentialId).HasMaxLength(256);
            entity.Property(e => e.UserAgent).HasMaxLength(512);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);
            entity.Property(e => e.Browser).HasMaxLength(100);
            entity.Property(e => e.TrustStatus).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<DeviceIpAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DeviceId, e.IpAddress }).IsUnique();
            entity.HasOne(e => e.Device).WithMany(d => d.IpHistory).HasForeignKey(e => e.DeviceId);
            entity.Property(e => e.IpAddress).HasMaxLength(64);
        });

        modelBuilder.Entity<IpAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Ip).IsUnique();
            entity.Property(e => e.Ip).HasMaxLength(64);
            entity.Property(e => e.CountryCode).HasMaxLength(8);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(100);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Isp).HasMaxLength(200);
            entity.Property(e => e.Organization).HasMaxLength(200);
            entity.Property(e => e.Asn).HasMaxLength(64);
            entity.Property(e => e.ThreatLevel).HasMaxLength(32);
            entity.Property(e => e.ReputationSource).HasMaxLength(100);
        });

        modelBuilder.Entity<IpUserAssociation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.IpAddressId, e.UserId }).IsUnique();
            entity.HasOne(e => e.IpAddress).WithMany(ip => ip.UserAssociations).HasForeignKey(e => e.IpAddressId);
            entity.HasOne(e => e.User).WithMany(u => u.IpAssociations).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<IpDeviceAssociation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.IpAddressId, e.DeviceId }).IsUnique();
            entity.HasOne(e => e.IpAddress).WithMany(ip => ip.DeviceAssociations).HasForeignKey(e => e.IpAddressId);
            entity.HasOne(e => e.Device).WithMany(d => d.IpAssociations).HasForeignKey(e => e.DeviceId);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenHash);
            entity.HasOne(e => e.User).WithMany(u => u.PasswordResetTokens).HasForeignKey(e => e.UserId);
            entity.Property(e => e.TokenHash).HasMaxLength(128);
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenHash);
            entity.HasOne(e => e.User).WithMany(u => u.EmailVerificationTokens).HasForeignKey(e => e.UserId);
            entity.Property(e => e.TokenHash).HasMaxLength(128);
        });
    }
}
