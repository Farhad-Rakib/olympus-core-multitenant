using Microsoft.AspNetCore.Authorization;

namespace OlympusCoreMultitenant.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {

        // Platform superadmin shortcut: a genuine platform-level admin (not tied to any tenant)
        // bypasses every permission check, including tenant-scoped ones.
        if (context.User.HasClaim(c => c.Type == "platform_admin" && c.Value == "true"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var hasPermission = context.User.Claims
            .Where(c => c.Type == "permission")
            .Any(c => string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
