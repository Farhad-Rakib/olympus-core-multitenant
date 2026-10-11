using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Infrastructure.Storage;

// Files are written to {RootPath}/{tenantId}/{random}{ext}.
//
// The tenant segment is what makes a stored file attributable and lets a tenant purge remove its
// files wholesale. The client's original filename is NEVER used on disk -- a fresh random name is
// generated instead, which removes path traversal, collisions and encoding tricks as a class rather
// than trying to sanitise them.
public sealed class LocalFileStorage : IFileStorage
{
    // A storage key is exactly "<digits>/<32 hex chars><extension>". Anything else is rejected
    // before it can reach the filesystem, so ".." and absolute paths never get a chance.
    private static readonly Regex StorageKeyPattern =
        new(@"^\d+/[0-9a-f]{32}\.[a-z0-9]{1,10}$", RegexOptions.Compiled);

    private readonly FileStorageOptions _options;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly string _rootPath;

    public LocalFileStorage(
        IOptions<FileStorageOptions> options,
        ICurrentTenantService currentTenantService,
        IHostEnvironment hostEnvironment)
    {
        _options = options.Value;
        _currentTenantService = currentTenantService;

        _rootPath = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(hostEnvironment.ContentRootPath, _options.RootPath);
    }

    public async Task<StoredFile> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new AppException("No tenant context is available for this upload.", 400);

        var extension = Path.GetExtension(originalFileName)?.ToLowerInvariant() ?? string.Empty;
        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new AppException($"File type '{extension}' is not allowed.", 400);
        }

        var tenantDirectory = Path.Combine(_rootPath, tenantId.ToString());
        Directory.CreateDirectory(tenantDirectory);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(tenantDirectory, fileName);

        await using (var target = File.Create(absolutePath))
        {
            await content.CopyToAsync(target, cancellationToken);
        }

        var storageKey = $"{tenantId}/{fileName}";
        var sizeBytes = new FileInfo(absolutePath).Length;

        return new StoredFile(storageKey, fileName, sizeBytes);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!TryResolve(storageKey, out var absolutePath) || !File.Exists(absolutePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(File.OpenRead(absolutePath));
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (!TryResolve(storageKey, out var absolutePath) || !File.Exists(absolutePath))
        {
            return Task.FromResult(false);
        }

        File.Delete(absolutePath);
        return Task.FromResult(true);
    }

    public Task<int> DeleteAllForTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        // Built from a long, so the segment cannot contain separators or traversal -- but the
        // containment check below is kept anyway rather than relying on that reasoning holding if
        // the key shape ever changes.
        var tenantDirectory = Path.GetFullPath(Path.Combine(_rootPath, tenantId.ToString()));
        var rootFullPath = Path.GetFullPath(_rootPath);

        if (!tenantDirectory.StartsWith(rootFullPath + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            || !Directory.Exists(tenantDirectory))
        {
            return Task.FromResult(0);
        }

        var removed = Directory.GetFiles(tenantDirectory, "*", SearchOption.AllDirectories).Length;
        Directory.Delete(tenantDirectory, recursive: true);

        return Task.FromResult(removed);
    }

    private bool TryResolve(string storageKey, out string absolutePath)
    {
        absolutePath = string.Empty;

        if (string.IsNullOrWhiteSpace(storageKey) || !StorageKeyPattern.IsMatch(storageKey))
        {
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(_rootPath, storageKey));

        // Belt and braces alongside the pattern check: even a key that satisfies the regex must
        // still resolve to somewhere inside the storage root.
        var rootFullPath = Path.GetFullPath(_rootPath);
        if (!candidate.StartsWith(rootFullPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return false;
        }

        absolutePath = candidate;
        return true;
    }
}
