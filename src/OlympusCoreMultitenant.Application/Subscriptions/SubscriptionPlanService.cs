using OlympusCoreMultitenant.Application.Common.Exceptions;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.Subscriptions.Dtos;
using OlympusCoreMultitenant.Domain.Entities;
using OlympusCoreMultitenant.Domain.Enums;

namespace OlympusCoreMultitenant.Application.Subscriptions;

public sealed class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly ISubscriptionPlanModuleRepository _subscriptionPlanModuleRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionPlanService(
        ISubscriptionPlanRepository subscriptionPlanRepository,
        ISubscriptionPlanModuleRepository subscriptionPlanModuleRepository,
        IModuleRepository moduleRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _subscriptionPlanModuleRepository = subscriptionPlanModuleRepository;
        _moduleRepository = moduleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _subscriptionPlanRepository.GetAllAsync(cancellationToken);
        var modules = await _moduleRepository.GetAllAsync(cancellationToken);
        var moduleKeysById = modules.ToDictionary(module => module.Id, module => module.Key);

        var result = new List<SubscriptionPlanDto>();
        foreach (var plan in plans)
        {
            var moduleIds = await _subscriptionPlanModuleRepository.GetModuleIdsForPlanAsync(plan.Id, cancellationToken);
            result.Add(MapPlan(plan, moduleIds, moduleKeysById));
        }

        return result;
    }

    public async Task<SubscriptionPlanDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var plan = await _subscriptionPlanRepository.GetByIdAsync(id, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var moduleIds = await _subscriptionPlanModuleRepository.GetModuleIdsForPlanAsync(id, cancellationToken);
        var modules = await _moduleRepository.GetAllAsync(cancellationToken);
        var moduleKeysById = modules.ToDictionary(module => module.Id, module => module.Key);

        return MapPlan(plan, moduleIds, moduleKeysById);
    }

    public async Task<SubscriptionPlanDto> CreateAsync(CreateSubscriptionPlanRequestDto request, CancellationToken cancellationToken = default)
    {
        var existing = await _subscriptionPlanRepository.GetByKeyAsync(request.Key.Trim().ToLowerInvariant(), cancellationToken);
        if (existing is not null)
        {
            throw new AppException("A subscription plan with this key already exists.", 409);
        }

        var plan = new SubscriptionPlan(request.Key, request.Name, request.Description, request.MaxUsers);
        await _subscriptionPlanRepository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var moduleIds = await ResolveModuleIdsAsync(request.ModuleKeys, cancellationToken);
        await _subscriptionPlanModuleRepository.ReplaceModulesAsync(plan.Id, moduleIds, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var modules = await _moduleRepository.GetAllAsync(cancellationToken);
        return MapPlan(plan, moduleIds, modules.ToDictionary(module => module.Id, module => module.Key));
    }

    public async Task<SubscriptionPlanDto> UpdateAsync(long id, UpdateSubscriptionPlanRequestDto request, CancellationToken cancellationToken = default)
    {
        var plan = await _subscriptionPlanRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Subscription plan not found.", 404);

        plan.Update(request.Name, request.Description, request.MaxUsers);
        _subscriptionPlanRepository.Update(plan);

        var moduleIds = await ResolveModuleIdsAsync(request.ModuleKeys, cancellationToken);
        await _subscriptionPlanModuleRepository.ReplaceModulesAsync(id, moduleIds, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var modules = await _moduleRepository.GetAllAsync(cancellationToken);
        return MapPlan(plan, moduleIds, modules.ToDictionary(module => module.Id, module => module.Key));
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var plan = await _subscriptionPlanRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppException("Subscription plan not found.", 404);

        if (await _subscriptionPlanRepository.HasAssignedTenantsAsync(id, cancellationToken))
        {
            throw new AppException("Cannot delete a subscription plan that is assigned to one or more tenants.", 409);
        }

        _subscriptionPlanRepository.Delete(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<long>> ResolveModuleIdsAsync(IReadOnlyList<string>? moduleKeys, CancellationToken cancellationToken)
    {
        if (moduleKeys is null || moduleKeys.Count == 0)
        {
            return Array.Empty<long>();
        }

        var requestedKeys = moduleKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var modules = await _moduleRepository.GetAllAsync(cancellationToken);

        return modules
            .Where(module => module.Kind != ModuleKind.System && requestedKeys.Contains(module.Key))
            .Select(module => module.Id)
            .ToList();
    }

    private static SubscriptionPlanDto MapPlan(SubscriptionPlan plan, IReadOnlyList<long> moduleIds, IReadOnlyDictionary<long, string> moduleKeysById) =>
        new(
            plan.Id,
            plan.Key,
            plan.Name,
            plan.Description,
            plan.MaxUsers,
            plan.IsActive,
            moduleIds.Where(moduleKeysById.ContainsKey).Select(id => moduleKeysById[id]).ToList());
}
