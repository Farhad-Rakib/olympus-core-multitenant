using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlympusCoreMultitenant.Api.Common;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Application.Subscriptions;
using OlympusCoreMultitenant.Application.Subscriptions.Dtos;

namespace OlympusCoreMultitenant.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/subscription-plans")]
[Authorize(Policy = Permissions.SubscriptionPlansManage)]
public sealed class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _subscriptionPlanService;

    public SubscriptionPlansController(ISubscriptionPlanService subscriptionPlanService)
    {
        _subscriptionPlanService = subscriptionPlanService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SubscriptionPlanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var plans = await _subscriptionPlanService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SubscriptionPlanDto>>.SuccessResponse(plans, "Subscription plans retrieved successfully"));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var plan = await _subscriptionPlanService.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return NotFound(ApiResponse.FailureResponse("Subscription plan not found", StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<SubscriptionPlanDto>.SuccessResponse(plan, "Subscription plan retrieved successfully"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionPlanDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanRequestDto request, CancellationToken cancellationToken)
    {
        var plan = await _subscriptionPlanService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, ApiResponse<SubscriptionPlanDto>.SuccessResponse(plan, "Subscription plan created successfully", StatusCodes.Status201Created));
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSubscriptionPlanRequestDto request, CancellationToken cancellationToken)
    {
        var plan = await _subscriptionPlanService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SubscriptionPlanDto>.SuccessResponse(plan, "Subscription plan updated successfully"));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _subscriptionPlanService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
