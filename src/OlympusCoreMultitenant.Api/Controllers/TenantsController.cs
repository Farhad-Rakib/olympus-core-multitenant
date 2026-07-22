using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlympusCoreMultitenant.Api.Common;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Application.Tenants;
using OlympusCoreMultitenant.Application.Tenants.Dtos;

namespace OlympusCoreMultitenant.Api.Controllers;

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
