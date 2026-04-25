namespace netcore_api_rbac_starter.Modules.Departments.Dtos;

public record CreateDepartmentRequest(string Name, string? Description);
public record UpdateDepartmentRequest(string? Name, string? Description);

public record DepartmentDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);