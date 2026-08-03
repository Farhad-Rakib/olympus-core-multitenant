using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    // Explicit TenantId filter: see UserRepository.GetByEmailAsync for why the automatic global
    // query filter is not sufficient for Include()+FirstOrDefaultAsync on this shape.
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await DbContext.RefreshTokens
            .Where(x => x.TenantId == DbContext.CurrentTenantId)
            .Include(x => x.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }
}
