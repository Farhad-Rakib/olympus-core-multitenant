using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Api.Middleware;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Tenants;
using ProjectNamePlaceholder.Application.Tenants.Dtos;

namespace ProjectNamePlaceholder.Api.Controllers;

// Self-scoped counterpart to TenantsController's admin endpoints (which require the platform-only
// tenants.manage permission): "my own tenant", not "any tenant by id". Route is singular
// (tenant/...) deliberately, to read distinctly from the plural admin surface. Any authenticated
// member of a tenant can call these -- there is nothing here a tenant's own users shouldn't be
// able to see about their own tenant.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenant")]
[Authorize]
public sealed class TenantSelfController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentTenantService _currentTenantService;

    public TenantSelfController(ITenantService tenantService, ICurrentTenantService currentTenantService)
    {
        _tenantService = tenantService;
        _currentTenantService = currentTenantService;
    }

    // [SkipLicenseCheck]: this is the one endpoint a tenant member must still be able to reach
    // once their own license is Expired/Suspended -- otherwise the very screen that would explain
    // why they're locked out is itself blocked by the 402 it exists to explain.
    [HttpGet("license")]
    [SkipLicenseCheck]
    [ProducesResponseType(typeof(ApiResponse<TenantLicenseStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLicenseStatus(CancellationToken cancellationToken)
    {
        var tenantId = _currentTenantService.TenantId
            ?? throw new AppException("No tenant context is available for this request.", 400);

        var status = await _tenantService.GetLicenseStatusAsync(tenantId, cancellationToken);
        return Ok(ApiResponse<TenantLicenseStatusDto>.SuccessResponse(new TenantLicenseStatusDto(status), "License status retrieved successfully"));
    }
}
