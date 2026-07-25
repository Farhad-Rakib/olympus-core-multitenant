using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlympusCoreMultitenant.Api.Common;
using OlympusCoreMultitenant.Api.Middleware;
using OlympusCoreMultitenant.Application.Tenants;
using OlympusCoreMultitenant.Application.Tenants.Dtos;

namespace OlympusCoreMultitenant.Api.Controllers;

// Anonymous, token-driven SUBMISSION of a tenant registration request -- the counterpart to the
// admin-triggered "Activate" action on TenantsController, but self-service via a shareable link.
// This never grants access by itself: submitting only records what the invitee entered as a
// pending request. A super admin must separately approve it (or activate directly) via
// TenantsController before the tenant is provisioned or the invitee can log in. Deliberately a
// separate controller (not nested under TenantsController) since every action here must be
// anonymous and un-tenant-scoped, the opposite of that controller's
// [Authorize(Policy = Permissions.TenantsManage)] gate.
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenant-registration")]
[AllowAnonymous]
[SkipTenantResolution]
public sealed class TenantRegistrationController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IValidator<SubmitTenantRegistrationRequestDto> _submitValidator;

    public TenantRegistrationController(ITenantService tenantService, IValidator<SubmitTenantRegistrationRequestDto> submitValidator)
    {
        _tenantService = tenantService;
        _submitValidator = submitValidator;
    }

    [HttpGet("{token}")]
    [ProducesResponseType(typeof(ApiResponse<TenantRegistrationInfoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfo(string token, CancellationToken cancellationToken)
    {
        var info = await _tenantService.GetRegistrationInfoAsync(token, cancellationToken);
        return Ok(ApiResponse<TenantRegistrationInfoDto>.SuccessResponse(info, "Registration link is valid"));
    }

    [HttpPost("submit")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit([FromBody] SubmitTenantRegistrationRequestDto request, CancellationToken cancellationToken)
    {
        await _submitValidator.ValidateAndThrowAsync(request, cancellationToken);
        await _tenantService.SubmitRegistrationAsync(request, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Your request has been submitted and is awaiting admin approval", StatusCodes.Status200OK));
    }
}
