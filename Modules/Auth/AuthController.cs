using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Auth.Dtos;
using netcore_api_rbac_starter.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace netcore_api_rbac_starter.Modules.Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, ICurrentUserService currentUser, IConfiguration configuration)
    {
        _authService = authService;
        _currentUser = currentUser;
        _configuration = configuration;
    }

    [EnableRateLimiting("login")]
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(Response<LoginResponse>.Ok(result, "Login successful."));
    }

    [EnableRateLimiting("login")]
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<Response<LoginResponse>>> Refresh([FromBody] RefreshRequest request)
    {
        var refreshToken = ResolveRefreshToken(request.RefreshToken);

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new BadHttpRequestException("Refresh token is required.");

        var result = await _authService.RefreshAsync(new RefreshRequest(refreshToken));
        SetRefreshTokenCookie(result.RefreshToken);
        return Ok(Response<LoginResponse>.Ok(result, "Token refreshed."));
    }

    [EnableRateLimiting("per-user")]
    [HttpPost("me")]
    [Authorize]
    public async Task<ActionResult<Response<MeResponse>>> Me()
    {
        var result = await _authService.GetMeAsync(_currentUser.UserId);
        return Ok(Response<MeResponse>.Ok(result));
    }

    [EnableRateLimiting("per-user")]
    [HttpGet("me/permissions")]
    [Authorize]
    public async Task<ActionResult<Response<IEnumerable<PermissionDto>>>> GetMyPermissions()
    {
        var result = await _authService.GetMyPermissionsAsync(_currentUser.UserId);
        return Ok(Response<IEnumerable<PermissionDto>>.Ok(result));
    }

    private string? ResolveRefreshToken(string? refreshToken)
        => !string.IsNullOrWhiteSpace(refreshToken)
            ? refreshToken
            : Request.Cookies[RefreshTokenCookieName];

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var expiryDays = _configuration.GetValue<int?>("Jwt:RefreshTokenExpiryDays") ?? 7;

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(expiryDays),
            Path = "/"
        });
    }
}
