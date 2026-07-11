using Microsoft.Extensions.Options;
using OlympusCoreMultitenant.Application.Auth.Dtos;
using OlympusCoreMultitenant.Application.Common.Configuration;
using OlympusCoreMultitenant.Application.Common.Exceptions;
using OlympusCoreMultitenant.Application.Common.Interfaces;
using OlympusCoreMultitenant.Application.Common.Interfaces.Security;
using OlympusCoreMultitenant.Application.Common.Interfaces.Services;
using OlympusCoreMultitenant.Application.Users.Dtos;
using OlympusCoreMultitenant.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace OlympusCoreMultitenant.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IPasswordResetTokenGenerator _passwordResetTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenantService _currentTenantService;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly AuthOptions _authOptions;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        ITenantRepository tenantRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IPasswordResetTokenGenerator passwordResetTokenGenerator,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ICurrentTenantService currentTenantService,
        ISystemSettingRepository systemSettingRepository,
        IOptions<AuthOptions> authOptions)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _tenantRepository = tenantRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _passwordResetTokenGenerator = passwordResetTokenGenerator;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _currentTenantService = currentTenantService;
        _systemSettingRepository = systemSettingRepository;
        _authOptions = authOptions.Value;
    }

    // The effective scope is stored as a global SystemSetting (admin-editable, no tenant context
    // needed to read it) and falls back to the appsettings.json default if no row exists yet.
    private async Task<EmailUniquenessScope> GetEffectiveScopeAsync(CancellationToken cancellationToken)
    {
        var setting = await _systemSettingRepository.GetByKeyAsync(SystemSettingKeys.AuthEmailUniquenessScope, cancellationToken);
        if (setting is not null && Enum.TryParse<EmailUniquenessScope>(setting.Value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return _authOptions.EmailUniquenessScope;
    }

    public async Task<LoginConfigDto> GetLoginConfigAsync(CancellationToken cancellationToken = default)
    {
        var scope = await GetEffectiveScopeAsync(cancellationToken);
        return new LoginConfigDto(scope == EmailUniquenessScope.PerTenant);
    }

    public async Task<AuthTokensDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var scope = await GetEffectiveScopeAsync(cancellationToken);
        return scope == EmailUniquenessScope.Global
            ? await LoginWithGlobalEmailAsync(request, cancellationToken)
            : await LoginWithTenantSlugAsync(request, cancellationToken);
    }

    // IMPORTANT: ICurrentTenantService is backed by AsyncLocal. Setting it inside a nested async
    // method that awaits something and then returns does NOT propagate the value back to the
    // caller once that method's Task completes (ExecutionContext forks on resume and the caller's
    // own captured context wins). So tenant resolution (BeginScope) and every subsequent
    // tenant-scoped query for a single login/register attempt must live in one unbroken async
    // flow — never split across a "resolve tenant" helper that returns before dependent queries run.
    private async Task<AuthTokensDto> LoginWithTenantSlugAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            throw new AppException("Tenant name is required to log in.", 400);
        }

        var slug = request.TenantSlug.Trim().ToLowerInvariant();
        var tenant = await _tenantRepository.GetBySlugAsync(slug, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            throw new AppException("Invalid credentials.", 401);
        }

        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash) || !user.IsActive)
        {
            throw new AppException("Invalid credentials.", 401);
        }

        var permissions = await _permissionRepository.GetPermissionNamesForUserAsync(user.Id, cancellationToken);
        return await GenerateAuthTokensAsync(user, permissions, cancellationToken);
    }

    private async Task<AuthTokensDto> LoginWithGlobalEmailAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAcrossTenantsAsync(request.Email, cancellationToken);
        if (user is null)
        {
            throw new AppException("Invalid credentials.", 401);
        }

        var tenant = await _tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            throw new AppException("Invalid credentials.", 401);
        }

        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash) || !user.IsActive)
        {
            throw new AppException("Invalid credentials.", 401);
        }

        var permissions = await _permissionRepository.GetPermissionNamesForUserAsync(user.Id, cancellationToken);
        return await GenerateAuthTokensAsync(user, permissions, cancellationToken);
    }

    public async Task<AuthTokensDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeSha256(request.RefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            throw new AppException("Invalid refresh token.", 401);
        }

        var user = await _userRepository.GetByIdWithRolesAsync(existingToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new AppException("User is not active.", 401);
        }

        var permissions = await _permissionRepository.GetPermissionNamesForUserAsync(user.Id, cancellationToken);

        var refreshToken = _refreshTokenGenerator.Generate();
        var newTokenHash = ComputeSha256(refreshToken.Token);
        existingToken.Revoke(newTokenHash);
        _refreshTokenRepository.Update(existingToken);

        await _refreshTokenRepository.AddAsync(new RefreshToken(user.Id, newTokenHash, refreshToken.ExpiresAtUtc), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, permissions);

        return new AuthTokensDto(
            accessToken,
            refreshToken.Token,
            _jwtTokenGenerator.GetAccessTokenExpiryUtc(),
            refreshToken.ExpiresAtUtc);
    }

    public async Task RevokeRefreshTokenAsync(RevokeRefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeSha256(request.RefreshToken);
        var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null)
        {
            return;
        }

        token.Revoke();
        _refreshTokenRepository.Update(token);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserRegistrationResult> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default)
    {
        // Tenant already ambient (authenticated admin creating a user in their own tenant via
        // middleware/JWT claim) -- register directly, no scope juggling needed.
        if (_currentTenantService.TenantId is not null)
        {
            return await RegisterInCurrentTenantAsync(request, cancellationToken);
        }

        // Anonymous self-signup: resolve the target tenant ourselves and keep the whole
        // registration in the same unbroken async flow as BeginScope (see LoginAsync comment).
        if (string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            throw new AppException("A valid tenant is required to register.", 400);
        }

        var slug = request.TenantSlug.Trim().ToLowerInvariant();
        var tenant = await _tenantRepository.GetBySlugAsync(slug, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            throw new AppException("A valid tenant is required to register.", 400);
        }

        using var scope = _currentTenantService.BeginScope(tenant.Id, tenant.Slug);
        return await RegisterInCurrentTenantAsync(request, cancellationToken);
    }

    private async Task<UserRegistrationResult> RegisterInCurrentTenantAsync(RegisterUserRequestDto request, CancellationToken cancellationToken)
    {
        var scope = await GetEffectiveScopeAsync(cancellationToken);
        var existing = scope == EmailUniquenessScope.Global
            ? await _userRepository.GetByEmailAcrossTenantsAsync(request.Email, cancellationToken)
            : await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            throw new AppException("Email is already registered.");
        }

        var hashedPassword = _passwordHasher.Hash(request.Password);
        var user = new User(request.FullName, request.Email, hashedPassword);

        var roles = await _roleRepository.GetByNamesAsync(request.Roles, cancellationToken);
        user.SetRoles(roles.Select(role => new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            User = user,
            Role = role
        }));

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserRegistrationResult(new UserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.IsActive,
            roles.Select(r => r.Name).ToList()));
    }

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(Dtos.ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        
        if (user is null || !user.IsActive)
        {
            // Don't reveal if user exists (security best practice)
            return new ForgotPasswordResponseDto("If an account exists with this email, a password reset link has been sent.");
        }

        // Generate password reset token
        var resetToken = _passwordResetTokenGenerator.Generate();
        var resetTokenHash = ComputeSha256(resetToken.Token);

        // Save the reset token
        await _passwordResetTokenRepository.AddAsync(
            new PasswordResetToken(user.Id, resetTokenHash, resetToken.ExpiresAtUtc),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send email with reset link
        var resetLink = $"https://localhost:3000/reset-password?token={resetToken.Token}&email={Uri.EscapeDataString(user.Email)}";
        var htmlBody = $@"
            <h2>Password Reset Request</h2>
            <p>Hi {user.FullName},</p>
            <p>You requested a password reset. Click the link below to reset your password:</p>
            <p><a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a></p>
            <p>This link will expire in 24 hours.</p>
            <p>If you didn't request this, please ignore this email.</p>
        ";

        await _emailService.SendAsync(user.Email, "Password Reset Request", htmlBody, cancellationToken);

        return new ForgotPasswordResponseDto("If an account exists with this email, a password reset link has been sent.");
    }

    public async Task ResetPasswordAsync(Dtos.ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new AppException("Invalid reset token or password.", 400);
        }

        var tokenHash = ComputeSha256(request.Token);
        var resetToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (resetToken is null || !resetToken.IsValid)
        {
            throw new AppException("Invalid or expired reset token.", 400);
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || user.Id != resetToken.UserId)
        {
            throw new AppException("Invalid reset token or email.", 400);
        }

        // Update password
        var hashedPassword = _passwordHasher.Hash(request.NewPassword);
        user.UpdatePassword(hashedPassword);
        
        // Mark reset token as used
        resetToken.MarkAsUsed();
        _passwordResetTokenRepository.Update(resetToken);
        
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send confirmation email
        var htmlBody = $@"
            <h2>Password Reset Successful</h2>
            <p>Hi {user.FullName},</p>
            <p>Your password has been successfully reset.</p>
            <p>If you didn't make this change, please contact support immediately.</p>
        ";

        await _emailService.SendAsync(user.Email, "Password Reset Confirmation", htmlBody, cancellationToken);
    }

    public async Task ChangePasswordAsync(Dtos.ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user is null || !user.IsActive)
        {
            throw new AppException("User not found or is inactive.", 404);
        }

        // Verify current password
        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new AppException("Current password is incorrect.", 401);
        }

        // Ensure new password is different from current
        if (request.CurrentPassword == request.NewPassword)
        {
            throw new AppException("New password must be different from current password.", 400);
        }

        // Update password
        var hashedPassword = _passwordHasher.Hash(request.NewPassword);
        user.UpdatePassword(hashedPassword);
        
        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send confirmation email
        var htmlBody = $@"
            <h2>Password Changed</h2>
            <p>Hi {user.FullName},</p>
            <p>Your password has been successfully changed.</p>
            <p>If you didn't make this change, please contact support immediately.</p>
        ";

        await _emailService.SendAsync(user.Email, "Password Changed", htmlBody, cancellationToken);
    }

    private async Task<AuthTokensDto> GenerateAuthTokensAsync(User user, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, permissions);
        var refreshToken = _refreshTokenGenerator.Generate();
        var refreshTokenHash = ComputeSha256(refreshToken.Token);

        await _refreshTokenRepository.AddAsync(new RefreshToken(user.Id, refreshTokenHash, refreshToken.ExpiresAtUtc), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(
            accessToken,
            refreshToken.Token,
            _jwtTokenGenerator.GetAccessTokenExpiryUtc(),
            refreshToken.ExpiresAtUtc);
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }
}
