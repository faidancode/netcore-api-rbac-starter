namespace netcore_api_rbac_starter.Modules.Users.Dtos;

public record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    Guid? RoleId,
    bool IsActive = true
);

public record UpdateUserRequest(
    string? Name,
    string? Email,
    string? Password,
    Guid? RoleId,
    bool? IsActive
);

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    bool IsActive,
    Guid? RoleId,
    string? RoleName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);