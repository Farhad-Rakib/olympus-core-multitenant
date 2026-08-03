using OlympusCoreMultitenant.Application.Common.Interfaces;

namespace OlympusCoreMultitenant.UnitTests.TestSupport;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}
