namespace ProjectNamePlaceholder.Application.Translations;

public interface ITranslationService
{
    Task<IReadOnlyList<string>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<TranslationBundleDto> GetBundleAsync(string language, CancellationToken cancellationToken = default);
    Task<UpsertTranslationsResultDto> UpsertAsync(string language, UpsertTranslationsRequestDto request, CancellationToken cancellationToken = default);
    Task<int> DeleteLanguageAsync(string language, CancellationToken cancellationToken = default);
}
