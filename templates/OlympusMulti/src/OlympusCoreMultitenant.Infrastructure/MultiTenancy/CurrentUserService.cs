using OlympusCoreMultitenant.Application.Common.Interfaces;

namespace OlympusCoreMultitenant.Infrastructure.MultiTenancy;

public sealed class CurrentUserService : ICurrentUserService
{
    private static readonly AsyncLocal<long?> _current = new();

    public long? UserId => _current.Value;

    public void Set(long userId)
    {
        _current.Value = userId;
    }
}
