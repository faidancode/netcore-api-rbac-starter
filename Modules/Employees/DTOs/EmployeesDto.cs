using netcore_api_rbac_starter.Entities;

namespace netcore_api_rbac_starter.Modules.Employees.Dtos;

public record CreateEmployeeRequest(
    string FullName,
    string Nip,
    Gender Gender,
    Guid PositionId,
    DateOnly DateOfJoining,
    DateOnly? DateOfActivePosition,
    EmployeeStatus EmployeeStatus = EmployeeStatus.Active,
    bool IsActive = true,
    Guid? UserId = null,
    Guid? DepartmentId = null,
    Guid? ManagerId = null
);

public record UpdateEmployeeRequest(
    string? FullName,
    string? Nip,
    Gender? Gender,
    Guid? PositionId,
    DateOnly? DateOfJoining,
    DateOnly? DateOfActivePosition,
    EmployeeStatus? EmployeeStatus,
    bool? IsActive,
    Guid? UserId,
    Guid? DepartmentId,
    Guid? ManagerId
);

public record EmployeeListQuery(
    string? Q = null,
    bool? IsActive = null,
    string? Sort = "createdAt:desc",
    int Page = 1,
    int Limit = 10
);

public record EmployeeDto(
    Guid Id,
    string FullName,
    string Nip,
    string Gender,
    string EmployeeStatus,
    bool IsActive,
    DateOnly DateOfJoining,
    DateOnly? DateOfActivePosition,
    Guid? UserId,
    string? UserName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid PositionId,
    string PositionName,
    Guid? ManagerId,
    string? ManagerName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record PositionHistoryDto(
    Guid Id,
    Guid EmployeeId,
    Guid PositionId,
    string PositionName,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsActive,
    string? Notes,
    DateTime CreatedAt
);