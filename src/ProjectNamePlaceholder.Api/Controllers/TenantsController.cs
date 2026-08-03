using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Application.Tenants;
using ProjectNamePlaceholder.Application.Tenants.Dtos;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenants")]
[Authorize(Policy = Permissions.TenantsManage)]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var tenants = await _tenantService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TenantDto>>.SuccessResponse(tenants, "Tenants retrieved successfully"));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return NotFound(ApiResponse.FailureResponse("Tenant not found", StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<TenantDto>.SuccessResponse(tenant, "Tenant retrieved successfully"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequestDto request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, ApiResponse<TenantDto>.SuccessResponse(tenant, "Tenant created successfully", StatusCodes.Status201Created));
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateTenantRequestDto request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<TenantDto>.SuccessResponse(tenant, "Tenant updated successfully"));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _tenantService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/provision")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Provision(long id, [FromBody] ProvisionTenantRequestDto? request, CancellationToken cancellationToken)
    {
        await _tenantService.ProvisionAsync(id, request?.ModuleKeys, request?.SubscriptionPlanId, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Tenant provisioned successfully", StatusCodes.Status200OK));
    }

    [HttpPost("{id:long}/registration-link")]
    [ProducesResponseType(typeof(ApiResponse<TenantRegistrationLinkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateRegistrationLink(long id, CancellationToken cancellationToken)
    {
        var link = await _tenantService.GenerateRegistrationLinkAsync(id, cancellationToken);
        return Ok(ApiResponse<TenantRegistrationLinkDto>.SuccessResponse(link, "Registration link generated successfully"));
    }

    [HttpGet("{id:long}/pending-registration")]
    [ProducesResponseType(typeof(ApiResponse<PendingTenantRegistrationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingRegistration(long id, CancellationToken cancellationToken)
    {
        var pending = await _tenantService.GetPendingRegistrationAsync(id, cancellationToken);
        return Ok(ApiResponse<PendingTenantRegistrationDto?>.SuccessResponse(pending, "Pending registration retrieved successfully"));
    }

    [HttpPost("{id:long}/approve-registration")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveRegistration(long id, [FromBody] ProvisionTenantRequestDto? request, CancellationToken cancellationToken)
    {
        await _tenantService.ApproveRegistrationAsync(id, request?.SubscriptionPlanId, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Registration approved and tenant activated successfully", StatusCodes.Status200OK));
    }

    [HttpPost("{id:long}/resync-catalog")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResyncCatalog(long id, CancellationToken cancellationToken)
    {
        await _tenantService.ReprovisionCatalogAsync(id, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Tenant catalog resynced successfully", StatusCodes.Status200OK));
    }

    [HttpPost("resync-catalog-all")]
    [ProducesResponseType(typeof(ApiResponse<TenantCatalogResyncResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResyncAllCatalogs(CancellationToken cancellationToken)
    {
        var result = await _tenantService.ReprovisionAllCatalogsAsync(cancellationToken);
        return Ok(ApiResponse<TenantCatalogResyncResultDto>.SuccessResponse(result, $"Resynced {result.ResyncedCount} tenant(s), skipped {result.SkippedCount} unprovisioned"));
    }

    [HttpGet("{id:long}/domains")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TenantDomainDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDomains(long id, CancellationToken cancellationToken)
    {
        var domains = await _tenantService.GetDomainsAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TenantDomainDto>>.SuccessResponse(domains, "Tenant domains retrieved successfully"));
    }

    [HttpPost("{id:long}/domains")]
    [ProducesResponseType(typeof(ApiResponse<TenantDomainDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddDomain(long id, [FromBody] AddTenantDomainRequestDto request, CancellationToken cancellationToken)
    {
        var domain = await _tenantService.AddDomainAsync(id, request.Domain, cancellationToken);
        return Ok(ApiResponse<TenantDomainDto>.SuccessResponse(domain, "Tenant domain added successfully"));
    }

    [HttpDelete("{id:long}/domains/{domainId:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveDomain(long id, long domainId, CancellationToken cancellationToken)
    {
        await _tenantService.RemoveDomainAsync(id, domainId, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Tenant domain removed successfully"));
    }

    [HttpGet("{id:long}/export")]
    [ProducesResponseType(typeof(ApiResponse<TenantExportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(long id, CancellationToken cancellationToken)
    {
        var export = await _tenantService.ExportAsync(id, cancellationToken);
        return Ok(ApiResponse<TenantExportDto>.SuccessResponse(export, "Tenant data exported successfully"));
    }

    // Irreversible. The tenant must already be disabled (via DELETE /tenants/{id}) and past its
    // cooling-off period, and the caller must pass the tenant's own slug back as confirmSlug --
    // see ITenantService.PurgeAsync.
    [HttpDelete("{id:long}/purge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Purge(long id, [FromQuery] string confirmSlug, CancellationToken cancellationToken)
    {
        await _tenantService.PurgeAsync(id, confirmSlug, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:long}/license")]
    [ProducesResponseType(typeof(ApiResponse<TenantLicenseStatusDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLicenseStatus(long id, CancellationToken cancellationToken)
    {
        var status = await _tenantService.GetLicenseStatusAsync(id, cancellationToken);
        return Ok(ApiResponse<TenantLicenseStatusDto>.SuccessResponse(new TenantLicenseStatusDto(status), "Tenant license status retrieved successfully"));
    }

    [HttpPost("{id:long}/license/trial")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartTrial(long id, [FromBody] StartTrialRequestDto request, CancellationToken cancellationToken)
    {
        await _tenantService.StartTrialAsync(id, request.TrialEndsAtUtc, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Trial started successfully"));
    }

    [HttpPost("{id:long}/license/expiry")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetLicenseExpiry(long id, [FromBody] SetLicenseExpiryRequestDto request, CancellationToken cancellationToken)
    {
        await _tenantService.SetLicenseExpiryAsync(id, request.LicenseExpiresAtUtc, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("License expiry updated successfully"));
    }

    [HttpPost("{id:long}/license/suspend")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SuspendLicense(long id, CancellationToken cancellationToken)
    {
        await _tenantService.SuspendLicenseAsync(id, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Tenant license suspended"));
    }

    [HttpPost("{id:long}/license/unsuspend")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UnsuspendLicense(long id, CancellationToken cancellationToken)
    {
        await _tenantService.UnsuspendLicenseAsync(id, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Tenant license unsuspended"));
    }

    [HttpPost("{id:long}/subscription-plan")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignSubscriptionPlan(long id, [FromBody] AssignSubscriptionPlanRequestDto request, CancellationToken cancellationToken)
    {
        await _tenantService.AssignSubscriptionPlanAsync(id, request.SubscriptionPlanId, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Subscription plan assigned successfully", StatusCodes.Status200OK));
    }

    [HttpGet("{id:long}/modules")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ModuleToggleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetModules(long id, CancellationToken cancellationToken)
    {
        var modules = await _tenantService.GetModuleEntitlementsAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ModuleToggleDto>>.SuccessResponse(modules, "Tenant modules retrieved successfully"));
    }

    [HttpPost("{id:long}/modules/{moduleId:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnableModule(long id, long moduleId, CancellationToken cancellationToken)
    {
        await _tenantService.EnableModuleAsync(id, moduleId, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Module enabled successfully", StatusCodes.Status200OK));
    }

    [HttpDelete("{id:long}/modules/{moduleId:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DisableModule(long id, long moduleId, CancellationToken cancellationToken)
    {
        await _tenantService.DisableModuleAsync(id, moduleId, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Module disabled successfully", StatusCodes.Status200OK));
    }
}
