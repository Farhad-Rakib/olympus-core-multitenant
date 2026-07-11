using Microsoft.EntityFrameworkCore;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Persistence.Context;

namespace OlympusCoreMultitenant.Persistence.Repositories;

public sealed class PasswordResetTokenRepository : BaseRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    // Explicit TenantId filter: see UserRepository.GetByEmailAsync for why the automatic global
    // query filter is not sufficient for Include()+FirstOrDefaultAsync on this shape.
    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PasswordResetToken>()
            .Where(x => x.TenantId == DbContext.CurrentTenantId)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<PasswordResetToken?> GetLatestByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PasswordResetToken>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
