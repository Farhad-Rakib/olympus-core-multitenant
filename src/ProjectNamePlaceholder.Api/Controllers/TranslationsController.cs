using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Api.Middleware;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Application.Translations;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/translations")]
public sealed class TranslationsController : ControllerBase
{
    private readonly ITranslationService _translationService;

    public TranslationsController(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    // Anonymous + optional tenant resolution, like auth/login-config: the login page must render
    // in the tenant's language before anyone signs in. Tenant comes from the host or X-Tenant-Id;
    // on the bare platform host there is none and the bundle is simply empty. Also
    // [SkipLicenseCheck]: a locked-out tenant's users still deserve readable lockout screens.
    [AllowAnonymous]
    [TenantResolutionOptional]
    [SkipLicenseCheck]
    [HttpGet("{language}")]
    [ProducesResponseType(typeof(ApiResponse<TranslationBundleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBundle(string language, CancellationToken cancellationToken)
    {
        var bundle = await _translationService.GetBundleAsync(language, cancellationToken);
        return Ok(ApiResponse<TranslationBundleDto>.SuccessResponse(bundle, "Translations retrieved successfully"));
    }

    [AllowAnonymous]
    [TenantResolutionOptional]
    [SkipLicenseCheck]
    [HttpGet("languages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLanguages(CancellationToken cancellationToken)
    {
        var languages = await _translationService.GetLanguagesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<string>>.SuccessResponse(languages, "Languages retrieved successfully"));
    }

    [Authorize(Policy = Permissions.TranslationsManage)]
    [HttpPut("{language}")]
    [ProducesResponseType(typeof(ApiResponse<UpsertTranslationsResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upsert(string language, [FromBody] UpsertTranslationsRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _translationService.UpsertAsync(language, request, cancellationToken);
        return Ok(ApiResponse<UpsertTranslationsResultDto>.SuccessResponse(result, "Translations saved successfully"));
    }

    [Authorize(Policy = Permissions.TranslationsManage)]
    [HttpDelete("{language}")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteLanguage(string language, CancellationToken cancellationToken)
    {
        var count = await _translationService.DeleteLanguageAsync(language, cancellationToken);
        return Ok(ApiResponse<int>.SuccessResponse(count, "Language removed successfully"));
    }
}
