using System.ComponentModel.DataAnnotations;

namespace netcore_api_rbac_starter.Modules.Users.Dtos;

public record CreateUserRequest(
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 2)]
    string Name,

    [Required]
    [EmailAddress]
    string Email,

    [Required]
    [StringLength(100, MinimumLength = 8)]
    string Password,

    [Required] // Usually RoleId is required for RBAC
    Guid? RoleId,

    bool IsActive = true
);

public record UpdateUserRequest(
    [StringLength(100, MinimumLength = 2)]
    string? Name,

    [EmailAddress]
    string? Email,

    [StringLength(100, MinimumLength = 8)]
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