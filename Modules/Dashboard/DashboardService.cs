using Microsoft.EntityFrameworkCore;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Modules.Dashboard.Dtos;

namespace netcore_api_rbac_starter.Modules.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var totalDepartments = await _db.Departments.CountAsync();
        var totalPositions = await _db.Positions.CountAsync();
        var totalActiveEmployees = await _db.Employees.CountAsync(e=>e.IsActive == true);
        var totalPermanentEmployees = await _db.Employees.CountAsync(e => e.EmploymentType == EmploymentType.Permanent);
        var totalContractEmployees = await _db.Employees.CountAsync(e => e.EmploymentType == EmploymentType.Contract);
        var totalMaleEmployees = await _db.Employees.CountAsync(e => e.Gender == Gender.Male);
        var totalFemaleEmployees = await _db.Employees.CountAsync(e => e.Gender == Gender.Female);

        return new DashboardSummaryDto(
            totalDepartments,
            totalPositions,
            totalActiveEmployees,
            totalPermanentEmployees,
            totalContractEmployees,
            totalMaleEmployees,
            totalFemaleEmployees
        );
    }
}
