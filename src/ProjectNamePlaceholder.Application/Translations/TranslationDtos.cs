namespace ProjectNamePlaceholder.Application.Translations;

public sealed record TranslationEntryDto(string Key, string Value);

// Flat "common.save" => "Enregistrer" map. The frontend nests it into i18next's resource shape;
// keeping it flat here makes the wire format trivially diffable and the admin form a plain table.
public sealed record TranslationBundleDto(string Language, IReadOnlyDictionary<string, string> Entries);

public sealed record UpsertTranslationsRequestDto(IReadOnlyList<TranslationEntryDto> Entries);

public sealed record UpsertTranslationsResultDto(int Created, int Updated, int Deleted);
