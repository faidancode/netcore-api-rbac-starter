using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Modules.Auth.Dtos;
using netcore_api_rbac_starter.Security;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter.Modules.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IJwtService jwt, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Role)
                .ThenInclude(r => r!.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        var permissions = user.Role?.RolePermissions.Select(rp => rp.Permission) ?? Enumerable.Empty<Permission>();
        var accessToken = _jwt.GenerateAccessToken(user, permissions);
        var refreshToken = _jwt.GenerateRefreshToken();

        var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
        });

        await _db.SaveChangesAsync();

        var permissionDtos = permissions.Select(p => new PermissionDto(p.Id, p.Action, p.Subject, p.Conditions, p.Fields));

        return new LoginResponse(
            accessToken,
            refreshToken,
            new UserInfo(user.Id, user.Name, user.Email, user.IsActive, user.Role?.Name),
            permissionDtos
        );
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request)
    {
        var storedToken = await _db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = storedToken.User;
        if (!user.IsActive)
            throw new UnauthorizedException("Account is deactivated.");

        // Revoke old token
        storedToken.IsRevoked = true;

        var permissions = user.Role?.RolePermissions.Select(rp => rp.Permission) ?? Enumerable.Empty<Permission>();
        var newAccessToken = _jwt.GenerateAccessToken(user, permissions);
        var newRefreshToken = _jwt.GenerateRefreshToken();

        var expiryDays = int.Parse(_config["Jwt:RefreshTokenExpiryDays"] ?? "7");

        _db.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
        });

        await _db.SaveChangesAsync();

        var permissionDtos = permissions.Select(p => new PermissionDto(p.Id, p.Action, p.Subject, p.Conditions, p.Fields));

        return new LoginResponse(
            newAccessToken,
            newRefreshToken,
            new UserInfo(user.Id, user.Name, user.Email, user.IsActive, user.Role?.Name),
            permissionDtos
        );
    }

    public async Task<MeResponse> GetMeAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.Role)
                .ThenInclude(r => r!.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User", userId);

        var permissions = user.Role?.RolePermissions
            .Select(rp => rp.Permission)
            .Select(p => new PermissionDto(p.Id, p.Action, p.Subject, p.Conditions, p.Fields))
            ?? Enumerable.Empty<PermissionDto>();

        return new MeResponse(user.Id, user.Name, user.Email, user.IsActive, user.Role?.Name, permissions);
    }

    public async Task<IEnumerable<PermissionDto>> GetMyPermissionsAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.Role)
                .ThenInclude(r => r!.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("User", userId);

        return user.Role?.RolePermissions
            .Select(rp => rp.Permission)
            .Select(p => new PermissionDto(p.Id, p.Action, p.Subject, p.Conditions, p.Fields))
            ?? Enumerable.Empty<PermissionDto>();
    }
}