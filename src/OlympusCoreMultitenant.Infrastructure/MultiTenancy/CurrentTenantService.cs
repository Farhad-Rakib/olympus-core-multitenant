using OlympusCoreMultitenant.Application.Common.Interfaces;

namespace OlympusCoreMultitenant.Infrastructure.MultiTenancy;

public sealed class CurrentTenantService : ICurrentTenantService
{
    private sealed record TenantContextData(long TenantId, string TenantSlug);

    private static readonly AsyncLocal<TenantContextData?> _current = new();

    public long? TenantId => _current.Value?.TenantId;
    public string? TenantSlug => _current.Value?.TenantSlug;

    public void Set(long tenantId, string tenantSlug)
    {
        _current.Value = new TenantContextData(tenantId, tenantSlug);
    }

    public IDisposable BeginScope(long tenantId, string tenantSlug)
    {
        var previous = _current.Value;
        _current.Value = new TenantContextData(tenantId, tenantSlug);
        return new TenantScope(previous);
    }

    public void Clear()
    {
        _current.Value = null;
    }

    private sealed class TenantScope : IDisposable
    {
        private readonly TenantContextData? _previous;
        private bool _disposed;

        public TenantScope(TenantContextData? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _current.Value = _previous;
            _disposed = true;
        }
    }
}
