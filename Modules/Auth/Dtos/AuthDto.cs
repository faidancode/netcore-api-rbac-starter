using System.ComponentModel.DataAnnotations;

namespace netcore_api_rbac_starter.Modules.Auth.Dtos;

public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

public record RefreshRequest([Required] string RefreshToken);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    UserInfo User,
    IEnumerable<PermissionDto> Permissions
);

public record UserInfo(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    string? RoleName
);

public record PermissionDto(
    Guid Id,
    string Action,
    string Subject,
    string? Conditions,
    string? Fields
);

public record MeResponse(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    string? RoleName,
    IEnumerable<PermissionDto> Permissions
);