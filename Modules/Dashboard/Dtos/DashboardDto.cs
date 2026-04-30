namespace netcore_api_rbac_starter.Modules.Dashboard.Dtos;

public record DashboardSummaryDto(
    int TotalDepartments,
    int TotalPositions,
    int TotalEmployees,
    int TotalPermanentEmployees,
    int TotalContractEmployees,
    int TotalMaleEmployees,
    int TotalFemaleEmployees
);
