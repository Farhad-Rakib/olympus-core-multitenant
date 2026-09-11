namespace ProjectNamePlaceholder.Application.Users;

// No refresh token on purpose: an impersonation session ends when the access token does.
public sealed record ImpersonationResultDto(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    long UserId,
    string FullName,
    string Email);

// Who is calling, as the controller reads it off the caller's own token.
public sealed record ImpersonationActor(long UserId, bool IsPlatformAdmin, bool IsAlreadyImpersonating);
