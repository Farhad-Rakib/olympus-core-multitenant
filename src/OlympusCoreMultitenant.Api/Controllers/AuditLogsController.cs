using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlympusCoreMultitenant.Api.Common;
using OlympusCoreMultitenant.Application.AuditLogs;
using OlympusCoreMultitenant.Application.Common.Dtos;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize(Policy = Permissions.AuditLogsRead)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
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
        var query = new AuditLogQuery
        {
            TenantId = tenantId,
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
