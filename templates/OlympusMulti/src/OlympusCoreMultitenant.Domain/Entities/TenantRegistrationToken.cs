using OlympusCoreMultitenant.Domain.Common;

namespace OlympusCoreMultitenant.Domain.Entities;

// Not an ITenantEntity: this is looked up anonymously before any tenant context exists (the
// invitee hasn't logged in, and the target tenant may not even be provisioned yet), so it must
// never be subject to the ambient-tenant query filter.
//
// Submitting via this token never provisions the tenant or creates a usable login by itself --
// it only records what the invitee entered (name/email/password hash) as a pending request. The
// tenant stays unprovisioned, and the invitee has no access, until a super admin reviews and
// approves it (or activates the tenant directly, ignoring the submission entirely).
public sealed class TenantRegistrationToken : BaseEntity
{
    public long TenantId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    public string? PendingFullName { get; private set; }
    public string? PendingEmail { get; private set; }
    public string? PendingPasswordHash { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }

    private TenantRegistrationToken()
    {
    }

    public TenantRegistrationToken(long tenantId, string token, DateTime expiresAtUtc)
    {
        TenantId = tenantId;
        Token = !string.IsNullOrWhiteSpace(token)
            ? token
            : throw new ArgumentException("Token is required.", nameof(token));
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool CanBeSubmitted => UsedAtUtc is null && SubmittedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    public bool IsPendingApproval => SubmittedAtUtc is not null && UsedAtUtc is null;

    public void Submit(string fullName, string email, string passwordHash)
    {
        PendingFullName = fullName;
        PendingEmail = email;
        PendingPasswordHash = passwordHash;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public void MarkUsed()
    {
        UsedAtUtc = DateTime.UtcNow;
    }
}
