using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Integration;
using ProjectNamePlaceholder.Application.Tenants.Events;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Users.EventHandlers;

// The Core-side reaction to a Platform-side fact: TenantService no longer needs to reference
// IUserRepository/IRoleRepository/User/UserRole/SystemRoles directly for this flow, only
// IEventBus + its own event record. Runs synchronously, in the same DI scope/DbContext as the
// publisher -- AddAsync only tracks the entity; the publisher's own SaveChangesAsync still commits
// it atomically alongside the rest of ApproveRegistrationAsync's work.
public sealed class CreateAdminUserOnTenantApprovedHandler : IIntegrationEventHandler<TenantApprovedIntegrationEvent>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public CreateAdminUserOnTenantApprovedHandler(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task HandleAsync(TenantApprovedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var adminRole = await _roleRepository.GetByNameAsync(SystemRoles.Admin, cancellationToken)
            ?? throw new AppException("Admin role is missing for this tenant.", 500);

        var adminUser = new User(integrationEvent.AdminFullName, integrationEvent.AdminEmail, integrationEvent.AdminPasswordHash);
        adminUser.SetRoles(new[]
        {
            new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id, User = adminUser, Role = adminRole }
        });

        await _userRepository.AddAsync(adminUser, cancellationToken);
    }
}
