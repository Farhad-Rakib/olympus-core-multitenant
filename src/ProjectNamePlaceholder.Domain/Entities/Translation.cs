using ProjectNamePlaceholder.Domain.Common;

namespace ProjectNamePlaceholder.Domain.Entities;

// One UI string override, per tenant, per language. Tenant-scoped like SiteSetting: wording is
// branding ("Sign in" vs "Log in to Acme"), so two tenants may translate the same key differently.
// Keys mirror the frontend's bundled en.json paths ("common.save"); anything missing here falls
// back to that bundle client-side, so a language never needs to be complete to be usable.
public sealed class Translation : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }

    // BCP 47 language tag, lowercased ("fr", "pt-br").
    public string Language { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
