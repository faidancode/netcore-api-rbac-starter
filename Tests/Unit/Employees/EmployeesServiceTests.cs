using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Employees;
using netcore_api_rbac_starter.Modules.Employees.Dtos;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Unit.Employees;

public class EmployeesServiceTests
{
    [Fact]
    public async Task Create_ValidRequest_ReturnsEmployeeDto()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new EmployeesService(db);

        var req = new CreateEmployeeRequest(
            FullName: "New Employee",
            Nip: "EMP-003",
            Gender: Gender.Male,
            PositionId: EntityBuilder.SeniorDevId,
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: new DateOnly(2023, 1, 1),
            DepartmentId: EntityBuilder.EngineeringId
        );

        var result = await svc.CreateAsync(req);

        result.Id.Should().NotBeEmpty();
        result.FullName.Should().Be("New Employee");
        result.Nip.Should().Be("EMP-003");
        result.PositionId.Should().Be(EntityBuilder.SeniorDevId);
        
        // Ensure position history is created
        var histories = await db.PositionHistories.Where(ph => ph.EmployeeId == result.Id).ToListAsync();
        histories.Should().HaveCount(1);
        histories.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_DuplicateNip_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new EmployeesService(db);

        var req = new CreateEmployeeRequest(
            FullName: "Copycat",
            Nip: "EMP-001", // Already exists
            Gender: Gender.Male,
            PositionId: EntityBuilder.SeniorDevId,
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: null
        );

        await Assert.ThrowsAsync<ConflictException>(() => svc.CreateAsync(req));
    }

    [Fact]
    public async Task GetAll_ReturnsPagedEmployees()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new EmployeesService(db);

        var result = await svc.GetAllAsync(new EmployeeListQuery());

        result.Total.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().Contain(e => e.Nip == "EMP-001");
        result.Items.Should().Contain(e => e.Nip == "EMP-002");
    }

    [Fact]
    public async Task GetById_ValidId_ReturnsEmployee()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new EmployeesService(db);

        var result = await svc.GetByIdAsync(EntityBuilder.Employee1Id);

        result.Id.Should().Be(EntityBuilder.Employee1Id);
        result.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = new EmployeesService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ValidRequest_UpdatesEmployee()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new EmployeesService(db);

        var req = new UpdateEmployeeRequest(
            FullName: "John Doe Updated",
            Nip: null,
            Gender: null,
            PositionId: null,
            DateOfJoining: null,
            DateOfActivePosition: null,
            EmployeeStatus: null,
            IsActive: null,
            UserId: null,
            DepartmentId: null,
            ManagerId: null
        );

        var result = await svc.UpdateAsync(EntityBuilder.Employee1Id, req);

        result.FullName.Should().Be("John Doe Updated");
        result.Nip.Should().Be("EMP-001"); // Unchanged
    }

    [Fact]
    public async Task Update_PositionChange_CreatesNewHistory()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new EmployeesService(db);

        var req = new UpdateEmployeeRequest(
            FullName: null,
            Nip: null,
            Gender: null,
            PositionId: EntityBuilder.HrManagerId, // Change position
            DateOfJoining: null,
            DateOfActivePosition: new DateOnly(2023, 6, 1),
            EmployeeStatus: null,
            IsActive: null,
            UserId: null,
            DepartmentId: null,
            ManagerId: null
        );

        var result = await svc.UpdateAsync(EntityBuilder.Employee1Id, req);

        result.PositionId.Should().Be(EntityBuilder.HrManagerId);
        
        var histories = await db.PositionHistories
            .Where(ph => ph.EmployeeId == EntityBuilder.Employee1Id)
            .OrderByDescending(ph => ph.StartDate)
            .ToListAsync();

        histories.Should().HaveCount(2);
        histories.First().IsActive.Should().BeTrue();
        histories.First().PositionId.Should().Be(EntityBuilder.HrManagerId);
        
        histories.Last().IsActive.Should().BeFalse();
        histories.Last().EndDate.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPositionHistories_ReturnsHistories()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new EmployeesService(db);

        var result = await svc.GetPositionHistoriesAsync(EntityBuilder.Employee1Id);

        result.Should().HaveCount(1);
        result.First().PositionId.Should().Be(EntityBuilder.SeniorDevId);
    }

    [Fact]
    public async Task Delete_ExistingEmployee_SoftDeletes()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new EmployeesService(db);

        await svc.DeleteAsync(EntityBuilder.Employee2Id);

        var emp = await db.Employees.IgnoreQueryFilters()
            .FirstAsync(e => e.Id == EntityBuilder.Employee2Id);

        emp.IsDeleted.Should().BeTrue();
        emp.DeletedAt.Should().NotBeNull();
    }
}
