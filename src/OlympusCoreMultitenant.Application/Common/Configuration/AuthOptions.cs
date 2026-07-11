namespace OlympusCoreMultitenant.Application.Common.Configuration;

public enum EmailUniquenessScope
{
    PerTenant,
    Global
}

public sealed class AuthOptions
{
    public EmailUniquenessScope EmailUniquenessScope { get; set; } = EmailUniquenessScope.PerTenant;
}
