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
        var totalEmployees = await _db.Employees.CountAsync();
        var totalMaleEmployees = await _db.Employees.CountAsync(e => e.Gender == Gender.Male);
        var totalFemaleEmployees = await _db.Employees.CountAsync(e => e.Gender == Gender.Female);

        return new DashboardSummaryDto(
            totalDepartments,
            totalPositions,
            totalEmployees,
            totalMaleEmployees,
            totalFemaleEmployees
        );
    }
}
