using Microsoft.Extensions.Options;
using OlympusCoreMultitenant.Application.Common;
using OlympusCoreMultitenant.Application.Common.Configuration;
using OlympusCoreMultitenant.Application.Common.Exceptions;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.FeatureFlags.Dtos;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.FeatureFlags;

public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly ITenantFeatureFlagRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppCache _cache;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly TimeSpan _cacheTtl;

    public FeatureFlagService(
        ITenantFeatureFlagRepository repository,
        IUnitOfWork unitOfWork,
        IAppCache cache,
        ICurrentTenantService currentTenantService,
        IOptions<CachingOptions> cachingOptions)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _currentTenantService = currentTenantService;
        _cacheTtl = TimeSpan.FromMinutes(cachingOptions.Value.FeatureFlagsTtlMinutes);
    }

    private long CurrentTenantId => _currentTenantService.TenantId ?? throw new TenantContextMissingException();

    private string CacheKey(string key) => TenantCacheKeys.For(CurrentTenantId, $"feature-flag:{key}");

    public async Task<bool> IsEnabledAsync(string key, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKey(key);
        var cached = await _cache.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached.Value;
        }

        var flag = await _repository.GetByKeyAsync(key, cancellationToken);
        var effective = flag is not null && flag.IsEnabled && IsWithinRollout(CurrentTenantId, key, flag.RolloutPercentage);

        await _cache.SetAsync(cacheKey, (bool?)effective, _cacheTtl, cancellationToken);
        return effective;
    }

    public async Task<IReadOnlyList<TenantFeatureFlagDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var flags = await _repository.GetAllAsync(cancellationToken);
        return flags.Select(MapToDto).ToList();
    }

    public async Task<TenantFeatureFlagDto> SetAsync(SetTenantFeatureFlagRequestDto request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByKeyAsync(request.Key, cancellationToken);
        if (existing is null)
        {
            existing = new TenantFeatureFlag
            {
                TenantId = CurrentTenantId,
                Key = request.Key,
                IsEnabled = request.IsEnabled,
                RolloutPercentage = request.RolloutPercentage
            };
            await _repository.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.IsEnabled = request.IsEnabled;
            existing.RolloutPercentage = request.RolloutPercentage;
            _repository.Update(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CacheKey(request.Key), cancellationToken);

        return MapToDto(existing);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var flag = await _repository.GetByIdAsync(id, cancellationToken);
        if (flag is null)
        {
            return;
        }

        _repository.Delete(flag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CacheKey(flag.Key), cancellationToken);
    }

    // Stable across processes and restarts (unlike string.GetHashCode, which is randomized per
    // process) so a tenant's rollout bucket for a given key never flips on redeploy.
    private static bool IsWithinRollout(long tenantId, string key, int rolloutPercentage)
    {
        if (rolloutPercentage >= 100)
        {
            return true;
        }

        if (rolloutPercentage <= 0)
        {
            return false;
        }

        var bucket = StableHash($"{tenantId}:{key}") % 100;
        return bucket < rolloutPercentage;
    }

    private static uint StableHash(string value)
    {
        unchecked
        {
            const uint fnvOffsetBasis = 2166136261;
            const uint fnvPrime = 16777619;
            var hash = fnvOffsetBasis;
            foreach (var ch in value)
            {
                hash ^= ch;
                hash *= fnvPrime;
            }

            return hash;
        }
    }

    private static TenantFeatureFlagDto MapToDto(TenantFeatureFlag flag) =>
        new(flag.Id, flag.Key, flag.IsEnabled, flag.RolloutPercentage);
}
