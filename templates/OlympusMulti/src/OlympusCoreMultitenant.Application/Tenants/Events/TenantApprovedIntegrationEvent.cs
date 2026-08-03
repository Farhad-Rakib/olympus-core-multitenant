using OlympusCoreMultitenant.Application.Integration;

namespace OlympusCoreMultitenant.Application.Tenants.Events;

// Published by TenantService.ApproveRegistrationAsync after provisioning, in place of directly
// constructing a User/Role -- the tenant/registration flow no longer needs to know how "create an
// admin account" is implemented, only that it happened. Carries the raw submitted values (already
// hashed) rather than a Tenant/User reference, so a handler needs no entity type this event's own
// module doesn't already own.
public sealed record TenantApprovedIntegrationEvent(
    long TenantId,
    string TenantSlug,
    string AdminFullName,
    string AdminEmail,
    string AdminPasswordHash) : IIntegrationEvent;
