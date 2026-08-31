using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Audit;
using SecurityGateway.Application.Audit.DTOs;
using SecurityGateway.Domain.Audit;
using SecurityGateway.Domain.Identity;
using SecurityGateway.Infrastructure.Audit.Repositories;
using SecurityGateway.Infrastructure.Audit.Services;
using SecurityGateway.Infrastructure.Persistence;
using Xunit;

namespace SecurityGateway.Tests.Audit;

public class AuditServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _service = new AuditService(new AuditLogRepository(_context), _context);
    }

    [Fact]
    public async Task LogAsync_PersistsLog()
    {
        await _service.LogAsync(AuditCategory.Authentication, "Login", Guid.NewGuid(), "admin", "1.1.1.1", "Success", true);

        var logs = await _service.SearchAsync(new AuditLogFilterRequest(), default);

        Assert.Single(logs);
        Assert.Equal("Login", logs[0].Action);
        Assert.Equal("admin", logs[0].Username);
    }

    [Fact]
    public async Task SearchAsync_FiltersByCategory()
    {
        var userId = Guid.NewGuid();
        await _service.LogAsync(AuditCategory.Authentication, "Login", userId, "admin", "1.1.1.1", null, true);
        await _service.LogAsync(AuditCategory.Blocking, "BlockIp", userId, "admin", "1.1.1.2", null, true);

        var logs = await _service.SearchAsync(new AuditLogFilterRequest { Category = AuditCategory.Blocking });

        Assert.Single(logs);
        Assert.Equal("BlockIp", logs[0].Action);
    }

    [Fact]
    public async Task SearchAsync_FiltersBySuccess()
    {
        await _service.LogAsync(AuditCategory.Authentication, "LoginFailed", null, null, "1.1.1.1", null, false);
        await _service.LogAsync(AuditCategory.Authentication, "Login", null, null, "1.1.1.1", null, true);

        var logs = await _service.SearchAsync(new AuditLogFilterRequest { Success = false });

        Assert.Single(logs);
        Assert.False(logs[0].Success);
    }

    [Fact]
    public async Task CountAsync_ReturnsTotal()
    {
        await _service.LogAsync(AuditCategory.Authentication, "Login", null, "admin", null, null, true);
        await _service.LogAsync(AuditCategory.Authentication, "Logout", null, "admin", null, null, true);

        var count = await _service.CountAsync(new AuditLogFilterRequest());

        Assert.Equal(2, count);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
