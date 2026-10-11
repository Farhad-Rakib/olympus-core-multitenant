namespace ProjectNamePlaceholder.Application.Common.Interfaces.Security;

// Extra claims stamped into an access token at issue time. Kept as a record so callers that don't
// care (login, refresh) pass null and get the historical token shape.
public sealed record AccessTokenOptions(
    // Plan-level request budget, emitted as the "plan_rpm" claim so the rate limiter can size a
    // tenant's bucket without a database read per request. Null = platform default.
    int? RequestsPerMinute = null,
    // Set when a support user is acting as this user: "act_by" carries the actor's id so every
    // audited write names the real person, and the frontend shows a persistent banner.
    long? ImpersonatorUserId = null,
    // Overrides Jwt:ExpiryMinutes; impersonation sessions are deliberately short.
    int? LifetimeMinutes = null);
