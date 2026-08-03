using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Tenants.Events;
using ProjectNamePlaceholder.Application.Users.EventHandlers;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Application;

public class CreateAdminUserOnTenantApprovedHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_a_user_assigned_the_Admin_role()
    {
        var roleRepository = new FakeRoleRepository();
        roleRepository.Seed(new Role(SystemRoles.Admin, "Administrative management access."));
        var userRepository = new FakeUserRepository();
        var handler = new CreateAdminUserOnTenantApprovedHandler(userRepository, roleRepository);

        await handler.HandleAsync(new TenantApprovedIntegrationEvent(1, "acme", "Ada Lovelace", "ada@acme.test", "hashed-password"));

        var created = Assert.Single(userRepository.Users);
        Assert.Equal("Ada Lovelace", created.FullName);
        Assert.Equal("ada@acme.test", created.Email);
        Assert.Contains(created.UserRoles, ur => ur.Role.Name == SystemRoles.Admin);
    }

    [Fact]
    public async Task HandleAsync_throws_when_the_tenant_has_no_Admin_role()
    {
        var roleRepository = new FakeRoleRepository();
        var userRepository = new FakeUserRepository();
        var handler = new CreateAdminUserOnTenantApprovedHandler(userRepository, roleRepository);

        await Assert.ThrowsAsync<AppException>(() =>
            handler.HandleAsync(new TenantApprovedIntegrationEvent(1, "acme", "Ada Lovelace", "ada@acme.test", "hashed-password")));
    }
}
