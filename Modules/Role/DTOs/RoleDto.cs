using netcore_api_rbac_starter.Modules.Auth.Dtos;

namespace netcore_api_rbac_starter.Modules.Roles.Dtos;

public record CreateRoleRequest(
    string Name,
    string? Description
);

public record UpdateRoleRequest(
    string? Name,
    string? Description
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