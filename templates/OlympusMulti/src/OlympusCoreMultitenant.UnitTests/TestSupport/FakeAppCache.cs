using System.Text.Json;
using OlympusCoreMultitenant.Application.Common.Interfaces;

namespace OlympusCoreMultitenant.UnitTests.TestSupport;

// Minimal in-memory stand-in for IAppCache (Redis/memory-cache in production) -- enough to prove
// FeatureFlagService reads/invalidates its cache correctly, without pulling in a mocking library
// for what's otherwise a two-method fake.
public sealed class FakeAppCache : IAppCache
{
    private readonly Dictionary<string, string> _store = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_store.TryGetValue(key, out var json))
        {
            return Task.FromResult<T?>(default);
        }

        return Task.FromResult(JsonSerializer.Deserialize<T>(json));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _store[key] = JsonSerializer.Serialize(value);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }
}
