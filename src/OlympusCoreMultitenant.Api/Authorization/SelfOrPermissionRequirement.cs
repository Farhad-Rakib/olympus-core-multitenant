using Microsoft.AspNetCore.Authorization;

namespace OlympusCoreMultitenant.Api.Authorization;

public sealed class SelfOrPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public string RouteParameterName { get; }

    public SelfOrPermissionRequirement(string permission, string routeParameterName = "id")
    {
        Permission = permission;
        RouteParameterName = routeParameterName;
    }
}
