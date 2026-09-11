using Microsoft.Extensions.Options;
using NSubstitute;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Common.Interfaces.Security;
using ProjectNamePlaceholder.Application.Users;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;
using ProjectNamePlaceholder.UnitTests.TestSupport;

namespace ProjectNamePlaceholder.UnitTests.Application;

public class ImpersonationServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly IPermissionRepository _permissions = Substitute.For<IPermissionRepository>();
    private readonly IJwtTokenGenerator _jwt = Substitute.For<IJwtTokenGenerator>();
    private readonly IAuditLogRepository _audit = Substitute.For<IAuditLogRepository>();
    private readonly ImpersonationService _service;

    public ImpersonationServiceTests()
    {
        _permissions.GetPermissionNamesForUserAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(new List<string> { "users.read" });
        _jwt.GenerateAccessToken(Arg.Any<User>(), Arg.Any<IEnumerable<string>>(), Arg.Any<AccessTokenOptions?>())
            .Returns("token");
        _service = new ImpersonationService(_users, _permissions, _jwt, _audit, new FakeUnitOfWork(),
            Options.Create(new AuthOptions { ImpersonationMinutes = 15 }));
    }

    private User AddUser(string email, bool platformAdmin = false, bool active = true)
    {
        var user = new User("Some One", email, "hash");
        if (platformAdmin) user.GrantPlatformSuperAdmin();
        if (!active) user.Disable();
        return _users.AddAsync(user).Result;
    }

    private static ImpersonationActor Actor(long id, bool platformAdmin = false, bool impersonating = false) =>
        new(id, platformAdmin, impersonating);

    [Fact]
    public async Task Issues_a_short_lived_token_naming_the_actor_and_audits_it()
    {
        var actor = AddUser("admin@acme.test");
        var target = AddUser("bob@acme.test");

        var result = await _service.ImpersonateAsync(target.Id, Actor(actor.Id));

        Assert.Equal("token", result.AccessToken);
        Assert.Equal(target.Email, result.Email);
        _jwt.Received(1).GenerateAccessToken(
            target,
            Arg.Any<IEnumerable<string>>(),
            Arg.Is<AccessTokenOptions>(o => o.ImpersonatorUserId == actor.Id && o.LifetimeMinutes == 15));
        await _audit.Received(1).AddAsync(
            Arg.Is<AuditLog>(a => a.Action == AuditAction.Accessed && a.UserId == actor.Id && a.EntityId == target.Id && a.EntityName == "UserImpersonation"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refuses_to_chain_impersonation()
    {
        var target = AddUser("bob@acme.test");

        var ex = await Assert.ThrowsAsync<AppException>(() => _service.ImpersonateAsync(target.Id, Actor(99, impersonating: true)));

        Assert.Equal(403, ex.StatusCode);
    }

    // The escalation this exists to stop: a tenant admin borrowing a platform admin's
    // pass-every-policy token.
    [Fact]
    public async Task Tenant_admin_cannot_impersonate_a_platform_admin()
    {
        var target = AddUser("root@platform.test", platformAdmin: true);

        var ex = await Assert.ThrowsAsync<AppException>(() => _service.ImpersonateAsync(target.Id, Actor(5)));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public async Task Platform_admin_may_impersonate_another_platform_admin()
    {
        var target = AddUser("root2@platform.test", platformAdmin: true);

        var result = await _service.ImpersonateAsync(target.Id, Actor(5, platformAdmin: true));

        Assert.Equal(target.Id, result.UserId);
    }

    [Fact]
    public async Task Inactive_or_missing_users_are_rejected()
    {
        var inactive = AddUser("gone@acme.test", active: false);

        Assert.Equal(409, (await Assert.ThrowsAsync<AppException>(() => _service.ImpersonateAsync(inactive.Id, Actor(5)))).StatusCode);
        Assert.Equal(404, (await Assert.ThrowsAsync<AppException>(() => _service.ImpersonateAsync(12345, Actor(5)))).StatusCode);
    }

    [Fact]
    public async Task Cannot_impersonate_yourself()
    {
        var me = AddUser("me@acme.test");

        var ex = await Assert.ThrowsAsync<AppException>(() => _service.ImpersonateAsync(me.Id, Actor(me.Id)));

        Assert.Equal(400, ex.StatusCode);
    }
}
