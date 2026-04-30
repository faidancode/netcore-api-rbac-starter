using System.ComponentModel.DataAnnotations;

namespace netcore_api_rbac_starter.Modules.Users.Dtos;

public record ListUsersQuery(
    string? Q = null,
    string? Search = null,
    int Page = 1,
    int Limit = 10,
    string Sort = "createdAt:desc"
);

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

    Guid? RoleId,

    bool? IsActive
);

public class ChangeUserPasswordRequest
{
    public string? CurrentPassword { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = default!;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; set; } = default!;
}

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
