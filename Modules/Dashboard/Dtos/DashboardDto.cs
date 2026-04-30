namespace netcore_api_rbac_starter.Modules.Dashboard.Dtos;

public record DashboardSummaryDto(
    int TotalDepartments,
    int TotalPositions,
    int TotalEmployees,
    int TotalMaleEmployees,
    int TotalFemaleEmployees
);
