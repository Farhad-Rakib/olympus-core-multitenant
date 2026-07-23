using OlympusCoreMultitenant.Application.Common.Dtos;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.AuditLogs;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRepository<Tenant> _tenantRepository;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        IUserRepository userRepository,
        IRepository<Tenant> tenantRepository)
    {
        _auditLogRepository = auditLogRepository;
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
    }

    public async Task<PagedResultDto<AuditLogDto>> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _auditLogRepository.GetPagedAsync(query, cancellationToken);

        var userIds = items.Where(log => log.UserId is not null).Select(log => log.UserId!.Value).Distinct().ToList();
        var users = userIds.Count > 0
            ? await _userRepository.GetByIdsAcrossTenantsAsync(userIds, cancellationToken)
            : Array.Empty<User>();
        var userNameById = users.ToDictionary(user => user.Id, user => user.FullName);

        var tenantIds = items.Where(log => log.TenantId is not null).Select(log => log.TenantId!.Value).ToHashSet();
        var tenants = tenantIds.Count > 0 ? await _tenantRepository.GetAllAsync(cancellationToken) : Array.Empty<Tenant>();
        var tenantNameById = tenants.Where(tenant => tenantIds.Contains(tenant.Id)).ToDictionary(tenant => tenant.Id, tenant => tenant.Name);

        var dtos = items.Select(log => new AuditLogDto(
            log.Id,
            log.TenantId,
            log.TenantId is not null && tenantNameById.TryGetValue(log.TenantId.Value, out var tenantName) ? tenantName : null,
            log.UserId,
            log.UserId is not null && userNameById.TryGetValue(log.UserId.Value, out var userName) ? userName : null,
            log.EntityName,
            log.EntityId,
            log.Action,
            log.Changes,
            log.CreatedAt)).ToList();

        var totalPages = query.PageSize > 0 ? (int)Math.Ceiling(total / (double)query.PageSize) : 0;

        return new PagedResultDto<AuditLogDto>(dtos, query.Page, query.PageSize, total, totalPages);
    }
}
