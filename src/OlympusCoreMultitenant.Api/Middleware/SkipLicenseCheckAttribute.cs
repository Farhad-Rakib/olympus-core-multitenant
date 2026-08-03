namespace OlympusCoreMultitenant.Api.Middleware;

public interface ISkipLicenseCheckMetadata
{
}

// For the narrow set of endpoints a tenant member must still be able to reach even when their own
// license is Expired/Suspended -- chiefly "what is my license status" itself, which would
// otherwise be blocked by the very 402 it exists to explain. Tenant resolution and context still
// run normally; only the license-status gate is skipped. Do not use this to bypass licensing for
// anything that isn't itself about explaining/resolving the license state.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SkipLicenseCheckAttribute : Attribute, ISkipLicenseCheckMetadata
{
}
