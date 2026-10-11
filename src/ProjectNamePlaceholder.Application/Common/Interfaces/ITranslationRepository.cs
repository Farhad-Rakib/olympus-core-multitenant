using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

// Every read is scoped to the ambient tenant by the global query filter on ITenantEntity.
public interface ITranslationRepository
{
    Task<IReadOnlyList<string>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Translation>> GetByLanguageAsync(string language, CancellationToken cancellationToken = default);
    Task AddAsync(Translation entity, CancellationToken cancellationToken = default);
    void Update(Translation entity);
    void Delete(Translation entity);
    Task<int> DeleteLanguageAsync(string language, CancellationToken cancellationToken = default);
}
