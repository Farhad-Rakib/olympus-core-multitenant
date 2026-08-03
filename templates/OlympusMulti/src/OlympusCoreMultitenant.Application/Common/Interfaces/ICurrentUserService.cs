namespace OlympusCoreMultitenant.Application.Common.Interfaces;

public interface ICurrentUserService
{
    long? UserId { get; }
    void Set(long userId);
}
