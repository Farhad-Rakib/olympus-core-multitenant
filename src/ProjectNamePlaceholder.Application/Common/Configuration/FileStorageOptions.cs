namespace ProjectNamePlaceholder.Application.Common.Configuration;

public sealed class FileStorageOptions
{
    // "local" is the only implementation today. Kept as a switch (like Payment:Provider) so an S3 or
    // Azure Blob IFileStorage can be added without touching callers.
    public string Provider { get; set; } = "local";

    // Relative paths resolve against the content root. Deliberately NOT wwwroot: files land outside
    // the static-file pipeline so nothing is served merely by existing on disk -- every read goes
    // through the controller.
    public string RootPath { get; set; } = "App_Data/uploads";

    // Absolute base for generated URLs (e.g. a CDN). When empty the URL is built from the incoming
    // request, which is what makes this work in development where the API and the frontend are on
    // different origins.
    public string? PublicBaseUrl { get; set; }

    public long MaxSizeBytes { get; set; } = 5 * 1024 * 1024;

    // Extension allowlist, matched case-insensitively. An allowlist, never a blocklist: a blocklist
    // is a guess about what is dangerous and is always incomplete.
    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
}
