using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlympusCoreMultitenant.Api.Common;
using OlympusCoreMultitenant.Application.FeatureFlags;
using OlympusCoreMultitenant.Application.FeatureFlags.Dtos;
using OlympusCoreMultitenant.Application.Security;

namespace OlympusCoreMultitenant.Api.Controllers;

// Feature flags for the caller's own (ambient) tenant -- there is no tenantId route parameter,
// unlike TenantsController's cross-tenant admin endpoints. A tenant admin manages their own
// flags; a platform admin can manage another tenant's via the existing X-Tenant-Id override.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/feature-flags")]
[Authorize]
public sealed class FeatureFlagsController : ControllerBase
{
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IValidator<SetTenantFeatureFlagRequestDto> _setValidator;

    public FeatureFlagsController(IFeatureFlagService featureFlagService, IValidator<SetTenantFeatureFlagRequestDto> setValidator)
    {
        _featureFlagService = featureFlagService;
        _setValidator = setValidator;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.FeatureFlagsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantFeatureFlagDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var flags = await _featureFlagService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TenantFeatureFlagDto>>.SuccessResponse(flags, "Feature flags retrieved successfully"));
    }

    [HttpGet("{key}/enabled")]
    [Authorize(Policy = Permissions.FeatureFlagsRead)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> IsEnabled(string key, CancellationToken cancellationToken)
    {
        var enabled = await _featureFlagService.IsEnabledAsync(key, cancellationToken);
        return Ok(ApiResponse<bool>.SuccessResponse(enabled, "Feature flag status retrieved successfully"));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.FeatureFlagsManage)]
    [ProducesResponseType(typeof(ApiResponse<TenantFeatureFlagDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Set([FromBody] SetTenantFeatureFlagRequestDto request, CancellationToken cancellationToken)
    {
        await _setValidator.ValidateAndThrowAsync(request, cancellationToken);
        var result = await _featureFlagService.SetAsync(request, cancellationToken);
        return Ok(ApiResponse<TenantFeatureFlagDto>.SuccessResponse(result, "Feature flag saved successfully"));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Permissions.FeatureFlagsManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _featureFlagService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Feature flag deleted successfully"));
    }
}
