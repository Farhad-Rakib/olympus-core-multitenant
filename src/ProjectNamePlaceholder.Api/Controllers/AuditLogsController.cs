using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.AuditLogs;
using ProjectNamePlaceholder.Application.Common.Dtos;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize(Policy = Permissions.AuditLogsReadOwn)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentTenantService _currentTenantService;

    public AuditLogsController(IAuditLogService auditLogService, ICurrentTenantService currentTenantService)
    {
        _auditLogService = auditLogService;
        _currentTenantService = currentTenantService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] long? tenantId,
        [FromQuery] string? entityName,
        [FromQuery] AuditAction? action,
        [FromQuery] long? userId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        // Platform admins keep the cross-tenant view (they hold audit-logs.read and legitimately
        // audit across tenants). Everyone else is pinned to their ambient tenant regardless of what
        // they passed as tenantId. Note AuditLog has no global query filter -- see AuditLogQuery.
        var isPlatformAdmin = User.HasClaim(c => c.Type == "platform_admin" && c.Value == "true");

        if (!isPlatformAdmin && _currentTenantService.TenantId is null)
        {
            // No ambient tenant and not a platform admin: there is no safe scope to fall back to,
            // and defaulting to "everything" is exactly the leak this guards against.
            return Forbid();
        }

        var query = new AuditLogQuery
        {
            TenantId = tenantId,
            EnforcedTenantId = isPlatformAdmin ? null : _currentTenantService.TenantId,
            EntityName = entityName,
            Action = action,
            UserId = userId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Page = page > 0 ? page : 1,
            PageSize = pageSize > 0 ? pageSize : 25
        };

        var result = await _auditLogService.GetPagedAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResultDto<AuditLogDto>>.SuccessResponse(result, "Audit logs retrieved successfully"));
    }
}
