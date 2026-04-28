using System.ComponentModel.DataAnnotations;

namespace netcore_api_rbac_starter.Modules.Positions.Dtos;

public record ListPositionQuery(
    string? Q = null,
    string? Search = null,
    int Page = 1,
    int Limit = 10,
    string Sort = "createdAt:desc"
);

public record CreatePositionRequest(
    // string Name, string? Description, Guid DepartmentId
    [Required(AllowEmptyStrings = false)]
    [StringLength(50, MinimumLength = 3)]
    string Name,

    [StringLength(250)]
    string? Description,

    [Required(AllowEmptyStrings = false)]
    Guid DepartmentId


    );
public record UpdatePositionRequest(
    [StringLength(50, MinimumLength = 3)]
    string? Name,

    [StringLength(250)]
    string? Description,

    [Required(AllowEmptyStrings = false)]
    Guid? DepartmentId


);

public record PositionDto(
    Guid Id,
    string Name,
    string? Description,
    Guid DepartmentId,
    string DepartmentName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);