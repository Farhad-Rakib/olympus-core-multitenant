using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Infrastructure.Email;

public sealed record ResolvedSmtpSettings(
    string Host,
    int Port,
    string Username,
    string Password,
    string FromAddress,
    string FromName,
    bool UseSsl);

public interface ITenantSmtpSettingsProvider
{
    Task<ResolvedSmtpSettings> ResolveAsync(CancellationToken cancellationToken = default);
}

// The tenant's own SiteSetting rows win over the Smtp:* appsettings block, so one tenant's mail
// goes out under its own domain/credentials instead of the platform's. Falls back per-key rather
// than all-or-nothing: a tenant that overrides only FromName still inherits the platform's host.
//
// SiteSetting is an ITenantEntity, so the repository read is already scoped to the ambient tenant
// by the global query filter -- no tenant id is passed here. With no tenant context (or no rows),
// every key falls back and the behaviour matches the pre-per-tenant behaviour exactly.
public sealed class TenantSmtpSettingsProvider : ITenantSmtpSettingsProvider
{
    private const string HostKey = "Smtp.Host";
    private const string PortKey = "Smtp.Port";
    private const string UsernameKey = "Smtp.Username";
    private const string PasswordKey = "Smtp.Password";
    private const string FromEmailKey = "Smtp.FromEmail";
    private const string FromNameKey = "Smtp.FromName";
    private const string UseSslKey = "Smtp.UseSsl";

    private readonly ISiteSettingRepository _siteSettingRepository;
    private readonly SmtpOptions _fallback;

    public TenantSmtpSettingsProvider(ISiteSettingRepository siteSettingRepository, IOptions<SmtpOptions> fallback)
    {
        _siteSettingRepository = siteSettingRepository;
        _fallback = fallback.Value;
    }

    public async Task<ResolvedSmtpSettings> ResolveAsync(CancellationToken cancellationToken = default)
    {
        // One read for the whole (small) per-tenant settings table beats six keyed round trips.
        var settings = await _siteSettingRepository.GetAllAsync(cancellationToken);

        var overrides = settings
            .Where(setting => !string.IsNullOrWhiteSpace(setting.Value))
            .GroupBy(setting => setting.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);

        return new ResolvedSmtpSettings(
            Host: Resolve(overrides, HostKey, _fallback.Host),
            Port: ResolveInt(overrides, PortKey, _fallback.Port),
            Username: Resolve(overrides, UsernameKey, _fallback.Username),
            Password: Resolve(overrides, PasswordKey, _fallback.Password),
            FromAddress: Resolve(overrides, FromEmailKey, _fallback.FromAddress),
            FromName: Resolve(overrides, FromNameKey, _fallback.FromName),
            UseSsl: ResolveBool(overrides, UseSslKey, _fallback.UseSsl));
    }

    private static string Resolve(IReadOnlyDictionary<string, string> overrides, string key, string fallback) =>
        overrides.TryGetValue(key, out var value) ? value : fallback;

    // A tenant admin can type anything into these free-text settings; an unparseable value falls
    // back to the platform default rather than failing the send.
    private static int ResolveInt(IReadOnlyDictionary<string, string> overrides, string key, int fallback) =>
        overrides.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : fallback;

    private static bool ResolveBool(IReadOnlyDictionary<string, string> overrides, string key, bool fallback) =>
        overrides.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;
}
