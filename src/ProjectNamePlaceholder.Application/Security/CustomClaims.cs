namespace ProjectNamePlaceholder.Application.Security;

// Non-standard JWT claim names shared by the token generator, the middleware that reads them,
// and the frontend (which decodes the same names). Change here, change everywhere.
public static class CustomClaims
{
    public const string TenantId = "tenant_id";
    public const string PlatformAdmin = "platform_admin";

    // Requests-per-minute budget of the tenant's subscription plan at token issue time.
    public const string PlanRequestsPerMinute = "plan_rpm";

    // Present only on an impersonation token: the user id of the support person acting as "sub".
    public const string ImpersonatedBy = "act_by";
}
