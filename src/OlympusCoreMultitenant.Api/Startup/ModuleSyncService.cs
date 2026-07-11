using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.Security;
using ModuleEntity = OlympusCoreMultitenant.Domain.Entities.Module;

namespace OlympusCoreMultitenant.Api.Startup;

public sealed class ModuleSyncService
{
    private readonly IModuleRepository _moduleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ModuleSyncService> _logger;

    public ModuleSyncService(IModuleRepository moduleRepository, IUnitOfWork unitOfWork, ILogger<ModuleSyncService> logger)
    {
        _moduleRepository = moduleRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _moduleRepository.GetAllAsync(cancellationToken);
            var existingKeys = new HashSet<string>(existing.Select(m => m.Key), StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<ModuleEntity>();
            foreach (var definition in Modules.All)
            {
                if (!existingKeys.Contains(definition.Key))
                {
                    toAdd.Add(new ModuleEntity(definition.Key, definition.Name, definition.Description, definition.Kind));
                }
            }

            if (toAdd.Count > 0)
            {
                _logger.LogInformation("Syncing {Count} new modules to database", toAdd.Count);
                foreach (var module in toAdd)
                {
                    await _moduleRepository.AddAsync(module, cancellationToken);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _logger.LogDebug("Modules already in sync");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync modules");
            throw;
        }
    }
}
