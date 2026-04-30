namespace netcore_api_rbac_starter.Modules.Departments.Dtos;

public record ListDepartmentQuery(
    string? Q = null,
    string? Search = null,
    int Page = 1,
    int Limit = 10,
    string Sort = "createdAt:desc"
);

public record CreateDepartmentRequest(
    string Name,

    string? Description
);
public record UpdateDepartmentRequest(
    string? Name,

    string? Description
);

public record DepartmentDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
