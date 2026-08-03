using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public sealed class ModuleRepository : BaseRepository<Module>, IModuleRepository
{
    public ModuleRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Module?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await DbContext.Modules
            .FirstOrDefaultAsync(module => module.Key == key, cancellationToken);
    }
}
