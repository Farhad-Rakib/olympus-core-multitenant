using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.AuditLogs;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPagedAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var filtered = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (query.TenantId is not null)
        {
            filtered = filtered.Where(log => log.TenantId == query.TenantId);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            filtered = filtered.Where(log => log.EntityName == query.EntityName);
        }

        if (query.Action is not null)
        {
            filtered = filtered.Where(log => log.Action == query.Action);
        }

        if (query.UserId is not null)
        {
            filtered = filtered.Where(log => log.UserId == query.UserId);
        }

        if (query.DateFrom is not null)
        {
            filtered = filtered.Where(log => log.CreatedAt >= query.DateFrom);
        }

        if (query.DateTo is not null)
        {
            filtered = filtered.Where(log => log.CreatedAt <= query.DateTo);
        }

        var total = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderByDescending(log => log.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
