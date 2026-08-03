namespace OlympusCoreMultitenant.Api.Middleware;

public interface ISkipTenantResolutionMetadata
{
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SkipTenantResolutionAttribute : Attribute, ISkipTenantResolutionMetadata
{
}
