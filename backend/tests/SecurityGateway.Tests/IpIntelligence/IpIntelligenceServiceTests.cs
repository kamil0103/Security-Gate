using SecurityGateway.Application.IpIntelligence;
using SecurityGateway.Domain.IpIntelligence;
using SecurityGateway.Infrastructure.IpIntelligence;
using SecurityGateway.Infrastructure.IpIntelligence.Providers;
using SecurityGateway.Tests.TestHelpers;
using Xunit;

namespace SecurityGateway.Tests.IpIntelligence;

public class IpIntelligenceServiceTests
{
    private readonly FakeIpAddressRepository _repository = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly IpIntelligenceService _service;

    public IpIntelligenceServiceTests()
    {
        _service = new IpIntelligenceService(
            _repository,
            new NullGeoIpProvider(),
            new NullReputationProvider(),
            new NullVpnProxyDetector(),
            _unitOfWork);
    }

    [Fact]
    public async Task TrackAsync_NewIp_CreatesAndReturnsDto()
    {
        var result = await _service.TrackAsync(new TrackIpRequest
        {
            IpAddress = "198.51.100.10"
        });

        Assert.NotNull(result);
        Assert.Equal("198.51.100.10", result.Ip);
        Assert.Equal(1L, result.RequestCount);
        Assert.True(_repository.Exists(result.Id));
        Assert.True(_unitOfWork.Saved);
    }

    [Fact]
    public async Task TrackAsync_ExistingIp_IncrementsRequestCountAndLastSeenAt()
    {
        var existing = new IpAddress { Ip = "198.51.100.10", RequestCount = 3 };
        _repository.Seed(existing);

        var firstSeen = existing.FirstSeenAt;
        await Task.Delay(10);

        var result = await _service.TrackAsync(new TrackIpRequest
        {
            IpAddress = "198.51.100.10"
        });

        Assert.Equal(4L, result.RequestCount);
        Assert.True(result.LastSeenAt > firstSeen);
    }

    [Fact]
    public async Task TrackAsync_WithUserId_CreatesUserAssociation()
    {
        var userId = Guid.NewGuid();

        var result = await _service.TrackAsync(new TrackIpRequest
        {
            IpAddress = "198.51.100.20",
            UserId = userId
        });

        var stored = _repository.GetByIp(result.Ip);
        Assert.NotNull(stored);
        Assert.Single(stored.UserAssociations);
        Assert.Equal(userId, stored.UserAssociations.First().UserId);
    }

    [Fact]
    public async Task TrackAsync_WithDeviceId_CreatesDeviceAssociation()
    {
        var deviceId = Guid.NewGuid();

        var result = await _service.TrackAsync(new TrackIpRequest
        {
            IpAddress = "198.51.100.30",
            DeviceId = deviceId
        });

        var stored = _repository.GetByIp(result.Ip);
        Assert.NotNull(stored);
        Assert.Single(stored.DeviceAssociations);
        Assert.Equal(deviceId, stored.DeviceAssociations.First().DeviceId);
    }

    [Fact]
    public async Task GetByIdAsync_Existing_ReturnsDto()
    {
        var ip = new IpAddress { Ip = "198.51.100.40" };
        _repository.Seed(ip);

        var result = await _service.GetByIdAsync(ip.Id);

        Assert.NotNull(result);
        Assert.Equal(ip.Ip, result.Ip);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsMostRecentlySeenFirst()
    {
        var older = new IpAddress { Ip = "198.51.100.50", LastSeenAt = DateTimeOffset.UtcNow.AddMinutes(-5) };
        var newer = new IpAddress { Ip = "198.51.100.60", LastSeenAt = DateTimeOffset.UtcNow };
        _repository.Seed(older);
        _repository.Seed(newer);

        var results = await _service.GetRecentAsync(10);

        Assert.Equal(2, results.Count);
        Assert.Equal(newer.Ip, results[0].Ip);
        Assert.Equal(older.Ip, results[1].Ip);
    }

    private sealed class FakeIpAddressRepository : IIpAddressRepository
    {
        private readonly List<IpAddress> _store = [];

        public void Seed(IpAddress ip) => _store.Add(ip);

        public IpAddress? GetByIp(string ip) => _store.FirstOrDefault(i => i.Ip == ip);

        public bool Exists(Guid id) => _store.Any(i => i.Id == id);

        public Task<IpAddress?> GetByIpAsync(string ip, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(GetByIp(ip));
        }

        public Task<IpAddress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_store.FirstOrDefault(i => i.Id == id));
        }

        public Task<IReadOnlyList<IpAddress>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
        {
            var sorted = _store
                .OrderByDescending(i => i.LastSeenAt)
                .Take(count)
                .ToList()
                .AsReadOnly();
            return Task.FromResult<IReadOnlyList<IpAddress>>(sorted);
        }

        public Task AddAsync(IpAddress ipAddress, CancellationToken cancellationToken = default)
        {
            _store.Add(ipAddress);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(IpAddress ipAddress, CancellationToken cancellationToken = default)
        {
            var existing = _store.FirstOrDefault(i => i.Id == ipAddress.Id);

            if (existing is not null)
            {
                _store.Remove(existing);
                _store.Add(ipAddress);
            }

            return Task.CompletedTask;
        }
    }
}
