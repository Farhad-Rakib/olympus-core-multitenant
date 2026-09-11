using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Api.Middleware;
using ProjectNamePlaceholder.Application.Billing;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Security;

namespace ProjectNamePlaceholder.Api.Controllers;

// Self-service billing for the ambient tenant, under the singular /tenant route like the other
// "my own tenant" endpoints. Every action is [SkipLicenseCheck] on purpose: an Expired or
// Suspended tenant is exactly the one that needs to reach checkout, and the 402 gate would
// otherwise lock them out of the only way to fix it. Reading the catalogue is open to any member
// (so a non-admin can see what to ask for); starting a checkout or opening the portal requires
// billing.manage, which the tenant's Admin role holds by default.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenant/billing")]
[Authorize]
[SkipLicenseCheck]
public sealed class TenantBillingController : ControllerBase
{
    private readonly ITenantBillingService _tenantBillingService;
    private readonly ICurrentTenantService _currentTenantService;

    public TenantBillingController(ITenantBillingService tenantBillingService, ICurrentTenantService currentTenantService)
    {
        _tenantBillingService = tenantBillingService;
        _currentTenantService = currentTenantService;
    }

    [HttpGet("config")]
    [ProducesResponseType(typeof(ApiResponse<TenantBillingConfigDto>), StatusCodes.Status200OK)]
    public IActionResult GetConfig()
    {
        var config = _tenantBillingService.GetConfig();
        return Ok(ApiResponse<TenantBillingConfigDto>.SuccessResponse(config, "Billing configuration retrieved successfully"));
    }

    [HttpGet("plans")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PurchasablePlanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var plans = await _tenantBillingService.GetPurchasablePlansAsync(RequireTenantId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PurchasablePlanDto>>.SuccessResponse(plans, "Plans retrieved successfully"));
    }

    [HttpPost("checkout")]
    [Authorize(Policy = Permissions.BillingManage)]
    [ProducesResponseType(typeof(ApiResponse<HostedUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> StartCheckout([FromBody] StartCheckoutRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _tenantBillingService.StartCheckoutAsync(RequireTenantId(), request.PlanKey, cancellationToken);
        return Ok(ApiResponse<HostedUrlDto>.SuccessResponse(result, "Checkout session created successfully"));
    }

    [HttpPost("portal")]
    [Authorize(Policy = Permissions.BillingManage)]
    [ProducesResponseType(typeof(ApiResponse<HostedUrlDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> OpenPortal(CancellationToken cancellationToken)
    {
        var result = await _tenantBillingService.OpenBillingPortalAsync(RequireTenantId(), cancellationToken);
        return Ok(ApiResponse<HostedUrlDto>.SuccessResponse(result, "Billing portal session created successfully"));
    }

    private long RequireTenantId() =>
        _currentTenantService.TenantId
            ?? throw new AppException("No tenant context is available for this request.", 400);
}
