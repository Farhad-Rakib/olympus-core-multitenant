using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.UnitTests.TestSupport;

public sealed class FakeSiteSettingRepository : ISiteSettingRepository
{
    private readonly List<SiteSetting> _settings = new();
    private long _nextId = 1;

    public FakeSiteSettingRepository(params (string Key, string Value)[] seed)
    {
        foreach (var (key, value) in seed)
        {
            _settings.Add(new SiteSetting { Id = _nextId++, Key = key, Value = value });
        }
    }

    public Task<SiteSetting> AddAsync(SiteSetting entity, CancellationToken cancellationToken = default)
    {
        entity.Id = _nextId++;
        _settings.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<SiteSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_settings.FirstOrDefault(s => s.Id == id));

    public Task<SiteSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_settings.FirstOrDefault(s => s.Key == key));

    public Task<IReadOnlyList<SiteSetting>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SiteSetting>>(_settings.ToList());

    public void Update(SiteSetting entity)
    {
        // No-op: the fake shares the same in-memory instance the caller already mutated.
    }

    public void Delete(SiteSetting entity)
    {
        _settings.RemoveAll(s => s.Id == entity.Id);
    }
}
