using System.ComponentModel.DataAnnotations;

namespace netcore_api_rbac_starter.Modules.Departments.Dtos;

public record CreateDepartmentRequest(
    [Required(AllowEmptyStrings = false)]
    [StringLength(50, MinimumLength = 3)]
    string Name,

    [StringLength(250)]
    string? Description
);
public record UpdateDepartmentRequest(
    [StringLength(50, MinimumLength = 3)]
    string? Name,

    [StringLength(250)]
    string? Description
);

public record DepartmentDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt
);