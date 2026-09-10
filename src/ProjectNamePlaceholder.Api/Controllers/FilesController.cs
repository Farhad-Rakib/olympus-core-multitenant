using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Api.Middleware;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Api.Controllers;

public sealed record UploadedFileDto(string Url, string FileName);

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/Files")]
public sealed class FilesController : ControllerBase
{
    // Content is validated by signature, not by the declared Content-Type or the extension, both of
    // which the client controls. A file whose bytes are not one of these is rejected outright.
    private static readonly Dictionary<string, byte[][]> MagicBytesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".gif"] = [[0x47, 0x49, 0x46, 0x38]],
        // WEBP is "RIFF....WEBP"; the four size bytes at offset 4 vary, so both parts are checked
        // separately below.
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]]
    };

    private static readonly Dictionary<string, string> ContentTypeByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp"
    };

    private readonly IFileStorage _fileStorage;
    private readonly FileStorageOptions _options;

    public FilesController(IFileStorage fileStorage, IOptions<FileStorageOptions> options)
    {
        _fileStorage = fileStorage;
        _options = options.Value;
    }

    [HttpPost("upload")]
    [Authorize]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(ApiResponse<UploadedFileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<UploadedFileDto>.FailureResponse("No file was uploaded."));
        }

        if (file.Length > _options.MaxSizeBytes)
        {
            return BadRequest(ApiResponse<UploadedFileDto>.FailureResponse(
                $"File exceeds the maximum size of {_options.MaxSizeBytes / 1024 / 1024} MB."));
        }

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<UploadedFileDto>.FailureResponse(
                $"File type '{extension}' is not allowed."));
        }

        await using var stream = file.OpenReadStream();
        if (!await HasMatchingSignatureAsync(stream, extension, cancellationToken))
        {
            return BadRequest(ApiResponse<UploadedFileDto>.FailureResponse(
                "File content does not match its extension."));
        }

        stream.Position = 0;
        var stored = await _fileStorage.SaveAsync(stream, file.FileName, cancellationToken);

        return Ok(ApiResponse<UploadedFileDto>.SuccessResponse(
            new UploadedFileDto(BuildUrl(stored.StorageKey), stored.FileName),
            "File uploaded successfully"));
    }

    // Anonymous by necessity: the URL is consumed by a plain <img src>, which cannot carry the
    // bearer token (it lives in web storage, not a cookie). Access therefore rests on the key being
    // unguessable -- a 122-bit random name inside a tenant folder. That is appropriate for avatars
    // and NOT appropriate for confidential documents: if this endpoint is ever reused for those,
    // switch to authenticated reads with a tenant check, or time-limited signed URLs.
    [HttpGet("{tenantId:long}/{fileName}")]
    [AllowAnonymous]
    [SkipTenantResolution]
    public async Task<IActionResult> Download(long tenantId, string fileName, CancellationToken cancellationToken)
    {
        var storageKey = $"{tenantId}/{fileName}";

        // Storage validates the key shape and confines it to the storage root; a traversal attempt
        // simply fails to resolve and is indistinguishable from a missing file.
        var stream = await _fileStorage.OpenReadAsync(storageKey, cancellationToken);
        if (stream is null)
        {
            return NotFound();
        }

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
        var contentType = ContentTypeByExtension.TryGetValue(extension, out var mapped)
            ? mapped
            : "application/octet-stream";

        return File(stream, contentType);
    }

    private static async Task<bool> HasMatchingSignatureAsync(Stream stream, string extension, CancellationToken cancellationToken)
    {
        if (!MagicBytesByExtension.TryGetValue(extension, out var signatures))
        {
            return false;
        }

        var header = new byte[12];
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        if (read < 4)
        {
            return false;
        }

        var matchesPrefix = signatures.Any(signature =>
            read >= signature.Length && header.Take(signature.Length).SequenceEqual(signature));

        if (!matchesPrefix)
        {
            return false;
        }

        // RIFF alone is also AVI/WAV, so a .webp must additionally carry "WEBP" at offset 8.
        if (extension is ".webp")
        {
            return read >= 12 && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;
        }

        return true;
    }

    private string BuildUrl(string storageKey)
    {
        var baseUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            // Absolute, not root-relative: in development the frontend is served from a different
            // origin than the API, so a relative URL in an <img> would resolve against the frontend.
            ? $"{Request.Scheme}://{Request.Host}"
            : _options.PublicBaseUrl.TrimEnd('/');

        return $"{baseUrl}/api/v1/Files/{storageKey}";
    }
}
