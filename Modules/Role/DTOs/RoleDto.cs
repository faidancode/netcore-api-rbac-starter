using netcore_api_rbac_starter.Modules.Auth.Dtos;

namespace netcore_api_rbac_starter.Modules.Roles.Dtos;

public record ListRoleQuery(
    string? Q = null,
    string? Search = null,
    int Page = 1,
    int Limit = 10,
    string Sort = "createdAt:desc"
);

public record CreateRoleRequest(
    string Name,

    string? Description,

    IEnumerable<Guid> PermissionIds
);

public record UpdateRoleRequest(
    string? Name,

    string? Description,

    IEnumerable<Guid> PermissionIds
);


public record AssignPermissionsRequest(
    IEnumerable<Guid> PermissionIds
);

public record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    IEnumerable<PermissionDto> Permissions,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
