using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public sealed class TranslationRepository : ITranslationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TranslationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<string>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Translations
            .AsNoTracking()
            .Select(t => t.Language)
            .Distinct()
            .OrderBy(l => l)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Translation>> GetByLanguageAsync(string language, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Translations
            .Where(t => t.Language == language)
            .OrderBy(t => t.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Translation entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.Translations.AddAsync(entity, cancellationToken);
    }

    public void Update(Translation entity)
    {
        _dbContext.Translations.Update(entity);
    }

    public void Delete(Translation entity)
    {
        _dbContext.Translations.Remove(entity);
    }

    public async Task<int> DeleteLanguageAsync(string language, CancellationToken cancellationToken = default)
    {
        // Tracked delete rather than ExecuteDelete: the latter bypasses SaveChanges and with it the
        // audit-log interceptor and tenant write stamping.
        var rows = await _dbContext.Translations.Where(t => t.Language == language).ToListAsync(cancellationToken);
        _dbContext.Translations.RemoveRange(rows);
        return rows.Count;
    }
}
