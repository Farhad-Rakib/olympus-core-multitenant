namespace OlympusCoreMultitenant.Application.Common.Interfaces.Security;

public interface IRefreshTokenGenerator
{
    RefreshTokenGenerationResult Generate();
}
