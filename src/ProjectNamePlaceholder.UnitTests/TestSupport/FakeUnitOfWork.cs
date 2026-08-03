using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.UnitTests.TestSupport;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}
