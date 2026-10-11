using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Application.Roles.Dtos;
using ProjectNamePlaceholder.Application.Users;
using ProjectNamePlaceholder.Application.Users.Dtos;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IImpersonationService _impersonationService;

    public UsersController(IUserService userService, IImpersonationService impersonationService)
    {
        _userService = userService;
        _impersonationService = impersonationService;
    }

    // Returns a short-lived token that is the target user, with an act_by claim naming the caller.
    // The frontend swaps its session to it and shows a persistent "viewing as" banner; the audit
    // log gets an Accessed row here and every write during the session is attributed to both.
    [HttpPost("{id:long}/impersonate")]
    [Authorize(Policy = Permissions.UsersImpersonate)]
    [ProducesResponseType(typeof(ApiResponse<ImpersonationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Impersonate(long id, CancellationToken cancellationToken)
    {
        var actor = new ImpersonationActor(
            GetCurrentUserId(),
            IsPlatformAdmin: User.HasClaim(c => c.Type == CustomClaims.PlatformAdmin && c.Value == "true"),
            IsAlreadyImpersonating: User.HasClaim(c => c.Type == CustomClaims.ImpersonatedBy));

        var result = await _impersonationService.ImpersonateAsync(id, actor, cancellationToken);
        return Ok(ApiResponse<ImpersonationResultDto>.SuccessResponse(result, "Impersonation session started"));
    }

    [HttpGet]
    [Authorize(Policy = Permissions.UsersRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserDto>>.SuccessResponse(users, "Users retrieved successfully"));
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = Policies.UsersReadOwnOrAny)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return NotFound(ApiResponse.FailureResponse("User not found", StatusCodes.Status404NotFound));

        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "User retrieved successfully"));
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await _userService.GetProfileAsync(userId, cancellationToken);
        if (profile is null)
            return NotFound(ApiResponse.FailureResponse("Profile not found", StatusCodes.Status404NotFound));

        return Ok(ApiResponse<ProfileDto>.SuccessResponse(profile, "Profile retrieved successfully"));
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<ProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var profile = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
        return Ok(ApiResponse<ProfileDto>.SuccessResponse(profile, "Profile updated successfully"));
    }

    [HttpGet("{id:long}/roles")]
    [Authorize(Policy = Permissions.UserRolesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(long id, CancellationToken cancellationToken)
    {
        var roles = await _userService.GetRolesAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.SuccessResponse(roles, "User roles retrieved successfully"));
    }

    [HttpPut("{id:long}/roles")]
    [Authorize(Policy = Permissions.UserRolesUpdate)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplaceUserRoles(long id, [FromBody] UpdateUserRolesRequestDto request, CancellationToken cancellationToken)
    {
        var user = await _userService.ReplaceRolesAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "User roles updated successfully"));
    }

    [HttpPost("{id:long}/roles/{roleId:long}")]
    [Authorize(Policy = Permissions.UserRolesCreate)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddUserRole(long id, long roleId, CancellationToken cancellationToken)
    {
        var user = await _userService.AddRoleAsync(id, roleId, cancellationToken);
        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "User role added successfully"));
    }

    [HttpDelete("{id:long}/roles/{roleId:long}")]
    [Authorize(Policy = Permissions.UserRolesDelete)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveUserRole(long id, long roleId, CancellationToken cancellationToken)
    {
        var user = await _userService.RemoveRoleAsync(id, roleId, cancellationToken);
        return Ok(ApiResponse<UserDto>.SuccessResponse(user, "User role removed successfully"));
    }
 
    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("id") ?? User.FindFirst("userId");
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("User ID claim not found.");
        return userId;
    }
}
