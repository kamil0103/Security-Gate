using Microsoft.EntityFrameworkCore;
using SecurityGateway.Application.Map;
using SecurityGateway.Application.Map.DTOs;
using SecurityGateway.Domain.IpIntelligence;
using SecurityGateway.Infrastructure.Map.Services;
using SecurityGateway.Infrastructure.Persistence;
using Xunit;

namespace SecurityGateway.Tests.Map;

public class MapServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly MapService _service;

    public MapServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _service = new MapService(_context);
    }

    [Fact]
    public async Task GetPointsAsync_ReturnsOnlyGeolocatedIps()
    {
        _context.IpAddresses.AddRange(
            new IpAddress { Id = Guid.NewGuid(), Ip = "1.1.1.1", Latitude = 51.5, Longitude = -0.1, ThreatScore = 10 },
            new IpAddress { Id = Guid.NewGuid(), Ip = "2.2.2.2", ThreatScore = 20 }
        );

        await _context.SaveChangesAsync();

        var result = await _service.GetPointsAsync(new MapFilterRequest());

        Assert.Single(result);
        Assert.Equal("1.1.1.1", result[0].IpAddress);
    }

    [Fact]
    public async Task GetPointsAsync_FiltersByCountryCode()
    {
        _context.IpAddresses.AddRange(
            new IpAddress { Id = Guid.NewGuid(), Ip = "1.1.1.1", CountryCode = "US", Latitude = 40.0, Longitude = -74.0, ThreatScore = 10 },
            new IpAddress { Id = Guid.NewGuid(), Ip = "2.2.2.2", CountryCode = "GB", Latitude = 51.5, Longitude = -0.1, ThreatScore = 20 }
        );

        await _context.SaveChangesAsync();

        var result = await _service.GetPointsAsync(new MapFilterRequest { CountryCode = "US" });

        Assert.Single(result);
        Assert.Equal("US", result[0].CountryCode);
    }

    [Fact]
    public async Task GetAttackPointsAsync_ReturnsOnlyIpsWithAttacks()
    {
        _context.IpAddresses.AddRange(
            new IpAddress { Id = Guid.NewGuid(), Ip = "1.1.1.1", Latitude = 40.0, Longitude = -74.0, AttackCount = 5 },
            new IpAddress { Id = Guid.NewGuid(), Ip = "2.2.2.2", Latitude = 51.5, Longitude = -0.1, AttackCount = 0 }
        );

        await _context.SaveChangesAsync();

        var result = await _service.GetAttackPointsAsync(new MapFilterRequest());

        Assert.Single(result);
        Assert.Equal("1.1.1.1", result[0].IpAddress);
    }

    [Fact]
    public async Task GetIpDetailsAsync_ReturnsDetails()
    {
        _context.IpAddresses.Add(new IpAddress
        {
            Id = Guid.NewGuid(),
            Ip = "8.8.8.8",
            Country = "United States",
            CountryCode = "US",
            City = "Mountain View",
            Latitude = 37.4,
            Longitude = -122.1,
            ThreatScore = 5
        });

        await _context.SaveChangesAsync();

        var result = await _service.GetIpDetailsAsync("8.8.8.8");

        Assert.NotNull(result);
        Assert.Equal("United States", result.Country);
        Assert.Equal(5, result.ThreatScore);
    }

    [Fact]
    public async Task GetIpDetailsAsync_UnknownIp_ReturnsNull()
    {
        var result = await _service.GetIpDetailsAsync("0.0.0.0");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCountriesAsync_ReturnsDistinctCountries()
    {
        _context.IpAddresses.AddRange(
            new IpAddress { Id = Guid.NewGuid(), Ip = "1.1.1.1", Country = "United States" },
            new IpAddress { Id = Guid.NewGuid(), Ip = "2.2.2.2", Country = "United States" },
            new IpAddress { Id = Guid.NewGuid(), Ip = "3.3.3.3", Country = "Germany" }
        );

        await _context.SaveChangesAsync();

        var result = await _service.GetCountriesAsync();

        Assert.Equal(2, result.Count);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
