namespace ProjectNamePlaceholder.Domain.Entities;

// Idempotency ledger for billing webhooks. Deliberately not ITenantEntity: a webhook arrives with
// no tenant context (it is authenticated by signature, not by JWT), so this table is written before
// any tenant scope exists. The unique index on ExternalEventId is what actually enforces
// exactly-once processing -- two concurrent deliveries of the same event race, and the loser's
// insert fails rather than double-applying a licence extension.
public sealed class ProcessedBillingEvent
{
    public long Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalEventId { get; set; } = string.Empty;
    public long TenantId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
