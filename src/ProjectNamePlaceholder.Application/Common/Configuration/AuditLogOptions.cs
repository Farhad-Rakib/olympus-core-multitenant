namespace ProjectNamePlaceholder.Application.Common.Configuration;

public sealed class AuditLogOptions
{
    // 0 disables cleanup entirely and keeps every row forever -- the old behaviour, kept available
    // because some regulated deployments are required to retain the full trail. Any positive value
    // deletes rows older than this many days.
    public int RetentionDays { get; set; }

    // How often the cleanup sweep runs. It also runs once shortly after startup so a deployment
    // that has been accumulating rows does not wait a full interval for the first pass.
    public int CleanupIntervalHours { get; set; } = 24;

    // Rows deleted per statement. Deliberately batched: audit_logs carries three indexes, and a
    // single unbounded DELETE against a large, actively-written table holds locks long enough to
    // stall ordinary requests. Small batches keep each transaction short.
    public int CleanupBatchSize { get; set; } = 1000;

    // Safety valve on one sweep, so a first run against years of backlog cannot saturate the
    // database for hours. Whatever is left is picked up by the next sweep.
    public int MaxBatchesPerRun { get; set; } = 50;
}
