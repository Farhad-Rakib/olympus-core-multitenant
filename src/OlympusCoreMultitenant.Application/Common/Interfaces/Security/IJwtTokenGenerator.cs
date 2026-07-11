using OlympusCoreMultitenant.Domain.Entities;

namespace OlympusCoreMultitenant.Application.Common.Interfaces.Security;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user, IEnumerable<string> permissions);
    DateTime GetAccessTokenExpiryUtc();
}
