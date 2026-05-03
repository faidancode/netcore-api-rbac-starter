using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Employees;
using netcore_api_rbac_starter.Modules.Employees.Dtos;
using netcore_api_rbac_starter.Tests.Helpers;
using System.Threading;

namespace netcore_api_rbac_starter.Tests.Unit.Employees;

public class EmployeesServiceTests
{
    private readonly Mock<IEventDispatcher> _dispatcherMock = new();

    // Helper untuk menginisialisasi service
    private EmployeesService CreateSvc(AppDbContext db)
        => new(db, _dispatcherMock.Object);

    // Helper untuk membuat CreateEmployeeRequest default
    private CreateEmployeeRequest CreateDefaultRequest(
        string nip = "EMP-003",
        Guid? positionId = null,
        string name = "New Employee")
    {
        return new CreateEmployeeRequest(
            FullName: name,
            Nip: nip,
            Gender: Gender.Male,
            PositionId: positionId ?? EntityBuilder.SeniorDevId,
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: new DateOnly(2023, 1, 1),
            DepartmentId: EntityBuilder.EngineeringId
        );
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsEmployeeDto()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateSvc(db);
        var req = CreateDefaultRequest();

        var result = await svc.CreateAsync(req, CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.FullName.Should().Be(req.FullName);
        result.Nip.Should().Be(req.Nip);

        var histories = await db.PositionHistories.Where(ph => ph.EmployeeId == result.Id).ToListAsync();
        histories.Should().HaveCount(1);
        histories.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_DuplicateNip_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateSvc(db);
        var req = CreateDefaultRequest(nip: "EMP-001"); // Already exists in Seed

        await Assert.ThrowsAsync<ConflictException>(() => svc.CreateAsync(req, CancellationToken.None));
    }

    [Fact]
    public async Task GetAll_ReturnsPagedEmployees()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateSvc(db);

        var result = await svc.GetAllAsync(new EmployeeListQuery(), CancellationToken.None);

        result.Total.Should().BeGreaterThanOrEqualTo(2);
        result.Items.Should().Contain(e => e.Nip == "EMP-001");
    }

    [Fact]
    public async Task GetById_ValidId_ReturnsEmployee()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateSvc(db);

        var result = await svc.GetByIdAsync(EntityBuilder.Employee1Id, CancellationToken.None);

        result.Id.Should().Be(EntityBuilder.Employee1Id);
        result.FullName.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Update_ValidRequest_UpdatesEmployee()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateSvc(db);

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

        var result = await svc.UpdateAsync(EntityBuilder.Employee1Id, req, CancellationToken.None);

        result.FullName.Should().Be("John Doe Updated");
        result.Nip.Should().Be("EMP-001");
    }

    [Fact]
    public async Task Update_PositionChange_CreatesNewHistory()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateSvc(db);

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

        var result = await svc.UpdateAsync(EntityBuilder.Employee1Id, req, CancellationToken.None);

        result.PositionId.Should().Be(EntityBuilder.HrManagerId);

        var histories = await db.PositionHistories
            .Where(ph => ph.EmployeeId == EntityBuilder.Employee1Id)
            .OrderByDescending(ph => ph.StartDate)
            .ToListAsync();

        histories.Should().HaveCount(2);
        histories.First().IsActive.Should().BeTrue();
        histories.Last().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetPositionHistories_ReturnsHistories()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateSvc(db);

        var result = await svc.GetPositionHistoriesAsync(EntityBuilder.Employee1Id, CancellationToken.None);

        result.Should().NotBeEmpty();
        result.First().PositionId.Should().Be(EntityBuilder.SeniorDevId);
    }

    [Fact]
    public async Task Delete_ExistingEmployee_SoftDeletes()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateSvc(db);

        await svc.DeleteAsync(EntityBuilder.Employee2Id, CancellationToken.None);

        var emp = await db.Employees.IgnoreQueryFilters()
            .FirstAsync(e => e.Id == EntityBuilder.Employee2Id);

        emp.IsDeleted.Should().BeTrue();
    }
}