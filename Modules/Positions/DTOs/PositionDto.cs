namespace netcore_api_rbac_starter.Modules.Positions.Dtos;

public record CreatePositionRequest(string Name, string? Description, Guid DepartmentId);
public record UpdatePositionRequest(string? Name, string? Description, Guid? DepartmentId);

public record PositionDto(
    Guid Id,
    string Name,
    string? Description,
    Guid DepartmentId,
    string DepartmentName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);