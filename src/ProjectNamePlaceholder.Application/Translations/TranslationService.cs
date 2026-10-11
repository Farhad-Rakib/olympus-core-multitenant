using System.Text.RegularExpressions;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Translations;

public sealed class TranslationService : ITranslationService
{
    // Bundles are read on every page load by every user and change only when an admin saves, so
    // they are cached per tenant+language and evicted on write.
    private const string CachePrefix = "translations:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private static readonly Regex LanguagePattern = new("^[a-z]{2,3}(-[a-z0-9]{2,8})*$", RegexOptions.Compiled);
    private static readonly Regex KeyPattern = new(@"^[A-Za-z0-9_]+(\.[A-Za-z0-9_]+)*$", RegexOptions.Compiled);

    private readonly ITranslationRepository _translationRepository;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly IAppCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    public TranslationService(
        ITranslationRepository translationRepository,
        ICurrentTenantService currentTenantService,
        IAppCache cache,
        IUnitOfWork unitOfWork)
    {
        _translationRepository = translationRepository;
        _currentTenantService = currentTenantService;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public Task<IReadOnlyList<string>> GetLanguagesAsync(CancellationToken cancellationToken = default) =>
        _translationRepository.GetLanguagesAsync(cancellationToken);

    public async Task<TranslationBundleDto> GetBundleAsync(string language, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeLanguage(language);

        // No tenant (bare platform host, anonymous) => nothing to override; the frontend's bundled
        // English applies. Returning an empty bundle rather than 404 keeps the client path uniform.
        if (_currentTenantService.TenantId is not { } tenantId)
        {
            return new TranslationBundleDto(normalized, new Dictionary<string, string>());
        }

        var cacheKey = CacheKey(tenantId, normalized);
        var cached = await _cache.GetAsync<Dictionary<string, string>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return new TranslationBundleDto(normalized, cached);
        }

        var rows = await _translationRepository.GetByLanguageAsync(normalized, cancellationToken);
        var entries = rows.ToDictionary(t => t.Key, t => t.Value, StringComparer.Ordinal);
        await _cache.SetAsync(cacheKey, entries, CacheTtl, cancellationToken);

        return new TranslationBundleDto(normalized, entries);
    }

    public async Task<UpsertTranslationsResultDto> UpsertAsync(string language, UpsertTranslationsRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeLanguage(language);
        var tenantId = _currentTenantService.TenantId
            ?? throw new AppException("No tenant context is available for this request.", 400);

        var incoming = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in request.Entries ?? [])
        {
            var key = entry.Key?.Trim() ?? string.Empty;
            if (!KeyPattern.IsMatch(key))
            {
                throw new AppException($"Invalid translation key '{entry.Key}'. Use dotted segments such as 'common.save'.", 400);
            }

            // An empty value means "no override for this key": the row is removed so the client
            // falls back to English, instead of rendering a blank label.
            incoming[key] = entry.Value ?? string.Empty;
        }

        var existing = (await _translationRepository.GetByLanguageAsync(normalized, cancellationToken))
            .ToDictionary(t => t.Key, StringComparer.Ordinal);

        int created = 0, updated = 0, deleted = 0;
        foreach (var (key, value) in incoming)
        {
            if (existing.TryGetValue(key, out var row))
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _translationRepository.Delete(row);
                    deleted++;
                }
                else if (!string.Equals(row.Value, value, StringComparison.Ordinal))
                {
                    row.Value = value;
                    _translationRepository.Update(row);
                    updated++;
                }
            }
            else if (!string.IsNullOrWhiteSpace(value))
            {
                await _translationRepository.AddAsync(new Translation
                {
                    TenantId = tenantId,
                    Language = normalized,
                    Key = key,
                    Value = value,
                }, cancellationToken);
                created++;
            }
        }

        if (created + updated + deleted > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cache.RemoveAsync(CacheKey(tenantId, normalized), cancellationToken);
        }

        return new UpsertTranslationsResultDto(created, updated, deleted);
    }

    public async Task<int> DeleteLanguageAsync(string language, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeLanguage(language);
        var tenantId = _currentTenantService.TenantId
            ?? throw new AppException("No tenant context is available for this request.", 400);

        var count = await _translationRepository.DeleteLanguageAsync(normalized, cancellationToken);
        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _cache.RemoveAsync(CacheKey(tenantId, normalized), cancellationToken);
        }

        return count;
    }

    private static string CacheKey(long tenantId, string language) => $"{CachePrefix}{tenantId}:{language}";

    public static string NormalizeLanguage(string language)
    {
        var normalized = language?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!LanguagePattern.IsMatch(normalized))
        {
            throw new AppException($"Invalid language tag '{language}'. Use a BCP 47 tag such as 'fr' or 'pt-br'.", 400);
        }

        return normalized;
    }
}
