using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjectNamePlaceholder.Application.Common.Configuration;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Common.Interfaces.Security;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Application.Users;

// "What does this person see?" for support staff. Issues a short-lived token that IS the target
// user (their roles, permissions, tenant) plus an act_by claim naming the actor, so every audited
// write during the session still identifies the real person behind it.
public sealed class ImpersonationService : IImpersonationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AuthOptions _authOptions;

    public ImpersonationService(
        IUserRepository userRepository,
        IPermissionRepository permissionRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions)
    {
        _userRepository = userRepository;
        _permissionRepository = permissionRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions.Value;
    }

    public async Task<ImpersonationResultDto> ImpersonateAsync(long targetUserId, ImpersonationActor actor, CancellationToken cancellationToken = default)
    {
        // No chaining: an impersonated session may not impersonate further, or the act_by trail
        // would stop naming the real person.
        if (actor.IsAlreadyImpersonating)
        {
            throw new AppException("You cannot impersonate another user while already impersonating.", 403);
        }

        if (actor.UserId == targetUserId)
        {
            throw new AppException("You are already signed in as this user.", 400);
        }

        // Scoped by the tenant query filter: a tenant admin only ever finds users of their own
        // tenant here; a platform admin reaches other tenants through the X-Tenant-Id override.
        var target = await _userRepository.GetByIdWithRolesAsync(targetUserId, cancellationToken)
            ?? throw new AppException("User not found.", 404);

        if (!target.IsActive)
        {
            throw new AppException("This user is inactive.", 409);
        }

        // A platform super admin's token passes every policy; handing that to a tenant admin via
        // impersonation would be a privilege escalation, so only another platform admin may do it.
        if (target.IsPlatformSuperAdmin && !actor.IsPlatformAdmin)
        {
            throw new AppException("Platform administrators cannot be impersonated.", 403);
        }

        var permissions = await _permissionRepository.GetPermissionNamesForUserAsync(target.Id, cancellationToken);
        var lifetime = _authOptions.ImpersonationMinutes > 0 ? _authOptions.ImpersonationMinutes : 30;

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(
            target,
            permissions,
            new AccessTokenOptions(ImpersonatorUserId: actor.UserId, LifetimeMinutes: lifetime));

        await _auditLogRepository.AddAsync(new AuditLog
        {
            TenantId = target.TenantId,
            UserId = actor.UserId,
            EntityName = "UserImpersonation",
            EntityId = target.Id,
            Action = AuditAction.Accessed,
            Changes = JsonSerializer.Serialize(new { TargetUserId = target.Id, target.Email, LifetimeMinutes = lifetime }),
            CreatedAt = DateTime.UtcNow,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImpersonationResultDto(
            accessToken,
            _jwtTokenGenerator.GetAccessTokenExpiryUtc(lifetime),
            target.Id,
            target.FullName,
            target.Email);
    }
}
