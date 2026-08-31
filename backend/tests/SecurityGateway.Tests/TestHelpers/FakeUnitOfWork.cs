using SecurityGateway.Application.Identity;

namespace SecurityGateway.Tests.TestHelpers;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public bool Saved { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Saved = true;
        return Task.FromResult(1);
    }
}
