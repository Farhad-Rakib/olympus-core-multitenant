using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OlympusCoreMultitenant.Api.Common;
using OlympusCoreMultitenant.Application.Security;
using OlympusCoreMultitenant.Application.SystemSettings;
using OlympusCoreMultitenant.Application.SystemSettings.Dtos;

namespace OlympusCoreMultitenant.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class SystemSettingsController : ControllerBase
{
    private readonly ISystemSettingService _systemSettingService;

    public SystemSettingsController(ISystemSettingService systemSettingService)
    {
        _systemSettingService = systemSettingService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SystemSettingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var settings = await _systemSettingService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SystemSettingDto>>.SuccessResponse(settings, "System settings retrieved successfully"));
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ApiResponse<SystemSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string key, CancellationToken cancellationToken)
    {
        var setting = await _systemSettingService.GetByKeyAsync(key, cancellationToken);
        if (setting is null)
            return NotFound(ApiResponse.FailureResponse("Setting not found", StatusCodes.Status404NotFound));

        return Ok(ApiResponse<SystemSettingDto>.SuccessResponse(setting, "System setting retrieved successfully"));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.SystemSettingsManage)]
    [ProducesResponseType(typeof(ApiResponse<SystemSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrUpdate([FromBody] SystemSettingDto dto, CancellationToken cancellationToken)
    {
        var result = await _systemSettingService.CreateOrUpdateAsync(dto, cancellationToken);
        return Ok(ApiResponse<SystemSettingDto>.SuccessResponse(result, "System setting saved successfully"));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Permissions.SystemSettingsManage)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _systemSettingService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("System setting deleted successfully"));
    }
}
