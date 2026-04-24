using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Auth.Dtos;
using netcore_api_rbac_starter.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace netcore_api_rbac_starter.Modules.Auth;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IAuthService authService, ICurrentUserService currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    /// <summary>Login with email and password</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Login successful."));
    }

    /// <summary>Refresh access token using a valid refresh token</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _authService.RefreshAsync(request);
        return Ok(ApiResponse<LoginResponse>.Ok(result, "Token refreshed."));
    }

    /// <summary>Get current authenticated user profile</summary>
    [HttpPost("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<MeResponse>>> Me()
    {
        var result = await _authService.GetMeAsync(_currentUser.UserId);
        return Ok(ApiResponse<MeResponse>.Ok(result));
    }

    /// <summary>Get current user permissions</summary>
    [HttpGet("me/permissions")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<IEnumerable<PermissionDto>>>> GetMyPermissions()
    {
        var result = await _authService.GetMyPermissionsAsync(_currentUser.UserId);
        return Ok(ApiResponse<IEnumerable<PermissionDto>>.Ok(result));
    }
}