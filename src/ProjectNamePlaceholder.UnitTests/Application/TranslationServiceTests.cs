using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Translations;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Application;

public class TranslationServiceTests
{
    private sealed class FakeTranslationRepository : ITranslationRepository
    {
        public List<Translation> Rows { get; } = new();
        private long _nextId = 1;

        public Task<IReadOnlyList<string>> GetLanguagesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<string>>(Rows.Select(r => r.Language).Distinct().OrderBy(l => l).ToList());

        public Task<IReadOnlyList<Translation>> GetByLanguageAsync(string language, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Translation>>(Rows.Where(r => r.Language == language).ToList());

        public Task AddAsync(Translation entity, CancellationToken ct = default)
        {
            entity.Id = _nextId++;
            Rows.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(Translation entity) { }
        public void Delete(Translation entity) => Rows.Remove(entity);

        public Task<int> DeleteLanguageAsync(string language, CancellationToken ct = default) =>
            Task.FromResult(Rows.RemoveAll(r => r.Language == language));
    }

    private sealed class StubTenant : ICurrentTenantService
    {
        public long? TenantId { get; set; } = 7;
        public string? TenantSlug => "acme";
        public void Set(long tenantId, string tenantSlug) => TenantId = tenantId;
        public IDisposable BeginScope(long tenantId, string tenantSlug) => throw new NotSupportedException();
        public void Clear() => TenantId = null;
    }

    private static (TranslationService service, FakeTranslationRepository repo, StubTenant tenant, FakeAppCache cache) Create()
    {
        var repo = new FakeTranslationRepository();
        var tenant = new StubTenant();
        var cache = new FakeAppCache();
        return (new TranslationService(repo, tenant, cache, new FakeUnitOfWork()), repo, tenant, cache);
    }

    private static UpsertTranslationsRequestDto Entries(params (string key, string value)[] entries) =>
        new(entries.Select(e => new TranslationEntryDto(e.key, e.value)).ToList());

    [Fact]
    public async Task Upsert_creates_updates_and_deletes_by_value()
    {
        var (service, repo, _, _) = Create();
        await service.UpsertAsync("fr", Entries(("common.save", "Enregistrer"), ("common.cancel", "Annuler")));

        var result = await service.UpsertAsync("FR", Entries(
            ("common.save", "Sauvegarder"),   // changed
            ("common.cancel", ""),            // blank => remove override, fall back to English
            ("common.delete", "Supprimer"))); // new

        Assert.Equal(new UpsertTranslationsResultDto(Created: 1, Updated: 1, Deleted: 1), result);
        var bundle = await service.GetBundleAsync("fr");
        Assert.Equal(new Dictionary<string, string> { ["common.save"] = "Sauvegarder", ["common.delete"] = "Supprimer" }, bundle.Entries);
        Assert.All(repo.Rows, r => Assert.Equal(7, r.TenantId));
    }

    [Fact]
    public async Task Bundle_is_cached_until_the_next_write()
    {
        var (service, repo, _, _) = Create();
        await service.UpsertAsync("fr", Entries(("common.save", "Enregistrer")));
        await service.GetBundleAsync("fr");

        // Mutate behind the service's back: a cached read must not see it, a write must evict.
        repo.Rows.Clear();
        Assert.Single((await service.GetBundleAsync("fr")).Entries);

        await service.UpsertAsync("fr", Entries(("common.cancel", "Annuler")));
        Assert.Equal(["common.cancel"], (await service.GetBundleAsync("fr")).Entries.Keys);
    }

    [Fact]
    public async Task Bundle_is_empty_with_no_ambient_tenant()
    {
        var (service, _, tenant, _) = Create();
        tenant.TenantId = null;

        var bundle = await service.GetBundleAsync("fr");

        Assert.Empty(bundle.Entries);
    }

    [Theory]
    [InlineData("")]
    [InlineData("french!")]
    [InlineData("languages")]
    public async Task Rejects_invalid_language_tags(string language)
    {
        var (service, _, _, _) = Create();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.GetBundleAsync(language));

        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [InlineData("common save")]
    [InlineData(".save")]
    [InlineData("common..save")]
    public async Task Rejects_invalid_keys(string key)
    {
        var (service, _, _, _) = Create();

        var ex = await Assert.ThrowsAsync<AppException>(() => service.UpsertAsync("fr", Entries((key, "x"))));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task Delete_language_removes_rows_and_evicts_cache()
    {
        var (service, _, _, _) = Create();
        await service.UpsertAsync("fr", Entries(("a.b", "1"), ("a.c", "2")));
        await service.GetBundleAsync("fr");

        var removed = await service.DeleteLanguageAsync("fr");

        Assert.Equal(2, removed);
        Assert.Empty((await service.GetBundleAsync("fr")).Entries);
        Assert.Empty(await service.GetLanguagesAsync());
    }
}
