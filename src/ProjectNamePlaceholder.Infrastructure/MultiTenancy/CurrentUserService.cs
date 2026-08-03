using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Infrastructure.MultiTenancy;

public sealed class CurrentUserService : ICurrentUserService
{
    private static readonly AsyncLocal<long?> _current = new();

    public long? UserId => _current.Value;

    public void Set(long userId)
    {
        _current.Value = userId;
    }
}
