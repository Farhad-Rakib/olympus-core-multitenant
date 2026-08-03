namespace OlympusCoreMultitenant.Application.Common;

public static class TenantCacheKeys
{
    public static string For(long tenantId, string key) => $"t:{tenantId}:{key}";
}
