namespace netcore_api_rbac_starter.Modules.Users.Dtos;

public record ListUsersQuery(
    string? Q = null,
    string? Search = null,
    int Page = 1,
    int Limit = 10,
    string Sort = "createdAt:desc"
);

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

    Guid? RoleId,

    bool? IsActive
);

public class ChangeUserPasswordRequest
{
    public string? CurrentPassword { get; set; }

    public string NewPassword { get; set; } = default!;

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
