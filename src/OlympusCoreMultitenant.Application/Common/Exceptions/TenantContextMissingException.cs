namespace OlympusCoreMultitenant.Application.Common.Exceptions;

public sealed class TenantContextMissingException : AppException
{
    public TenantContextMissingException()
        : base("No tenant context is available for this operation.", 500)
    {
    }
}
