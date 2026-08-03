namespace OlympusCoreMultitenant.Api.Middleware;

public interface IOptionalTenantResolutionMetadata
{
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class TenantResolutionOptionalAttribute : Attribute, IOptionalTenantResolutionMetadata
{
}
