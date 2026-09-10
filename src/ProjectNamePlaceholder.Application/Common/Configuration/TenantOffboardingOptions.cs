namespace ProjectNamePlaceholder.Application.Common.Configuration;

// What a hard purge does with the tenant's audit trail. There is no universally correct answer --
// audit integrity says keep the trail, data-protection law says erasure means erasure -- so this is
// an explicit choice rather than whatever falls out of the deletion order.
public enum PurgedTenantAuditHandling
{
    // Keep the trail, strip the personal data: UserId is nulled and the Changes payload is replaced
    // with a redaction marker. The shape of the history survives (what happened, when, to which
    // entity) without the names and email addresses that CaptureAuditEntries records for User rows.
    // The default because it satisfies an erasure request without destroying the audit record.
    Anonymize,

    // Keep everything, including PII. Only appropriate where a retention obligation outranks
    // erasure -- and where you can defend that choice.
    Retain,

    // Delete the rows outright. The cleanest erasure, and the most complete loss of history.
    Delete
}

// Uploaded files are pure tenant content with none of the audit trail's integrity value, so unlike
// PurgedTenantAuditHandling there is no "anonymize" middle ground -- the file either goes or stays.
public enum PurgedTenantFileHandling
{
    // Default: an erasure request means the tenant's uploads go too.
    Delete,

    // Keep them on disk. Only where a retention obligation applies -- note they become unreachable
    // through the app once the tenant is gone, so this is cold storage, not accessible data.
    Retain
}

public sealed class TenantOffboardingOptions
{
    // A tenant must have been soft-disabled for at least this long before it can be hard-deleted,
    // so a purge can never be the very next action after a disable -- there's always a window to
    // notice and reverse it via Enable() first.
    public int PurgeCoolingOffDays { get; set; } = 7;

    // Previously unspecified: audit rows simply survived the purge as orphans still holding user
    // names and emails, which is the one outcome nobody would choose deliberately.
    public PurgedTenantAuditHandling AuditHandling { get; set; } = PurgedTenantAuditHandling.Anonymize;

    public PurgedTenantFileHandling FileHandling { get; set; } = PurgedTenantFileHandling.Delete;
}
