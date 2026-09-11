namespace ProjectNamePlaceholder.Application.Users;

public interface IImpersonationService
{
    Task<ImpersonationResultDto> ImpersonateAsync(long targetUserId, ImpersonationActor actor, CancellationToken cancellationToken = default);
}
