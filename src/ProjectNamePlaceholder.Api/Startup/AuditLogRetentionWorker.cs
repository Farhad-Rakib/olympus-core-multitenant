using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Api.Startup;

// Runs the audit retention sweep on a timer. Registered unconditionally but exits immediately when
// retention is disabled, so enabling it is a config change rather than a redeploy.
public sealed class AuditLogRetentionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuditLogOptions _options;
    private readonly ILogger<AuditLogRetentionWorker> _logger;

    public AuditLogRetentionWorker(
        IServiceProvider serviceProvider,
        IOptions<AuditLogOptions> options,
        ILogger<AuditLogRetentionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.RetentionDays <= 0)
        {
            _logger.LogInformation("Audit log retention is disabled (AuditLog:RetentionDays is 0); rows are kept indefinitely.");
            return;
        }

        // Deliberately not immediate: startup already runs migrations, catalog sync and seeding, and
        // a bulk delete competing with those makes boot slower and harder to diagnose.
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        var interval = TimeSpan.FromHours(Math.Max(1, _options.CleanupIntervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // A BackgroundService is a singleton; ApplicationDbContext is scoped. A scope per
                // sweep also keeps the change tracker from living across runs.
                using var scope = _serviceProvider.CreateScope();
                var retentionService = scope.ServiceProvider.GetRequiredService<IAuditLogRetentionService>();

                var deleted = await retentionService.PurgeExpiredAsync(stoppingToken);

                if (deleted > 0)
                {
                    _logger.LogInformation(
                        "Audit log retention removed {DeletedCount} row(s) older than {RetentionDays} day(s).",
                        deleted, _options.RetentionDays);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let a failed sweep kill the worker -- the next interval retries, and a
                // maintenance job must not take the application down with it.
                _logger.LogError(ex, "Audit log retention sweep failed; will retry at the next interval.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
