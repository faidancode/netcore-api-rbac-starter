using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Dashboard;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Unit.Dashboard;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummary_ReturnsSeededCounts()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        var service = new DashboardService(db);
        var summary = await service.GetSummaryAsync();

        summary.TotalDepartments.Should().Be(2);
        summary.TotalPositions.Should().Be(2);
        summary.TotalActiveEmployees.Should().Be(2);
        summary.TotalMaleEmployees.Should().Be(1);
        summary.TotalFemaleEmployees.Should().Be(1);
    }

    [Fact]
    public async Task GetSummary_IgnoresSoftDeletedEntities()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        db.Departments.Add(new Department { Name = "Temporary" });
        db.Positions.Add(new Position { Name = "Temp Position", DepartmentId = EntityBuilder.EngineeringId });
        db.Employees.Add(new Employee
        {
            FullName = "Temp Male",
            Nip = "EMP-TEMP",
            Gender = Gender.Male,
            PositionId = EntityBuilder.SeniorDevId,
            DepartmentId = EntityBuilder.EngineeringId,
            DateOfJoining = new DateOnly(2024, 1, 1),
            DateOfActivePosition = new DateOnly(2024, 1, 1),
            EmployeeStatus = EmployeeStatus.Active,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var tempDepartment = await db.Departments.FirstAsync(d => d.Name == "Temporary");
        var tempPosition = await db.Positions.FirstAsync(p => p.Name == "Temp Position");
        var tempEmployee = await db.Employees.FirstAsync(e => e.Nip == "EMP-TEMP");

        tempDepartment.IsDeleted = true;
        tempPosition.IsDeleted = true;
        tempEmployee.IsDeleted = true;
        await db.SaveChangesAsync();

        var service = new DashboardService(db);
        var summary = await service.GetSummaryAsync();

        summary.TotalDepartments.Should().Be(2);
        summary.TotalPositions.Should().Be(2);
        summary.TotalActiveEmployees.Should().Be(2);
        summary.TotalMaleEmployees.Should().Be(1);
        summary.TotalFemaleEmployees.Should().Be(1);
    }
}
