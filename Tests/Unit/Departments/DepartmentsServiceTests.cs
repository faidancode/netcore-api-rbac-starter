using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Departments;
using netcore_api_rbac_starter.Modules.Departments.Dtos;
using netcore_api_rbac_starter.Modules.Departments.Validators;
using netcore_api_rbac_starter.Tests.Helpers;
using Moq;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Security;

namespace netcore_api_rbac_starter.Tests.Unit.Departments;

public class DepartmentsServiceTests
{
    private static DepartmentsService CreateService(
        AppDbContext db,
        ICurrentUserService? currentUser = null)
    {
        return new DepartmentsService(
            db,
            currentUser ?? new Mock<ICurrentUserService>().Object,
            NullLogger<DepartmentsService>.Instance);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsDepartmentDto()
    {
        await using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        var result = await svc.CreateAsync(new CreateDepartmentRequest("Finance", "Finance team"));

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Finance");
        result.Description.Should().Be("Finance team");
    }

    [Fact]
    public async Task Create_LogsRequestAndUserContext()
    {
        await using var db = DbContextFactory.Create();

        var loggerMock = new Mock<ILogger<DepartmentsService>>();
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.SetupGet(x => x.RequestId).Returns("req-123");
        currentUserMock.SetupGet(x => x.UserId).Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var svc = new DepartmentsService(db, currentUserMock.Object, loggerMock.Object);

        await svc.CreateAsync(new CreateDepartmentRequest("Finance", "Finance team"));

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("RequestId: req-123") &&
                    state.ToString()!.Contains("UserId: 11111111-1111-1111-1111-111111111111")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.CreateAsync(new CreateDepartmentRequest("Engineering", "Duplicate")));
    }

    [Fact]
    public async Task GetAll_ReturnsAllActiveDepartments()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateService(db);

        var pagedResult = await svc.GetAllAsync(new ListDepartmentQuery());
        var result = pagedResult.Items.ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Name == "Engineering");
        result.Should().Contain(d => d.Name == "Human Resources");
    }

    [Fact]
    public async Task GetAll_ExcludesSoftDeletedDepartments()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        var dept = await db.Departments.FirstAsync(d => d.Id == EntityBuilder.EngineeringId);
        dept.IsDeleted = true;
        await db.SaveChangesAsync();

        var svc = CreateService(db);
        var pagedResult = await svc.GetAllAsync(new ListDepartmentQuery());
        var result = pagedResult.Items.ToList();

        result.Should().HaveCount(1);
        result.Should().NotContain(d => d.Name == "Engineering");
    }

    [Fact]
    public async Task GetById_ValidId_ReturnsDepartment()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.GetByIdAsync(EntityBuilder.EngineeringId);

        result.Id.Should().Be(EntityBuilder.EngineeringId);
        result.Name.Should().Be("Engineering");
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ChangesNameAndDescription()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.UpdateAsync(EntityBuilder.EngineeringId,
            new UpdateDepartmentRequest("Platform", "Platform engineering"));

        result.Name.Should().Be("Platform");
        result.Description.Should().Be("Platform engineering");
    }

    [Fact]
    public async Task Update_DuplicateName_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.UpdateAsync(EntityBuilder.EngineeringId,
                new UpdateDepartmentRequest("Human Resources", null)));
    }

    [Fact]
    public async Task Update_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.UpdateAsync(Guid.NewGuid(), new UpdateDepartmentRequest("X", null)));
    }

    [Fact]
    public async Task Delete_ExistingDepartment_SoftDeletesDepartment()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = CreateService(db);

        await svc.DeleteAsync(EntityBuilder.HrDeptId);

        var dept = await db.Departments.IgnoreQueryFilters()
            .FirstAsync(d => d.Id == EntityBuilder.HrDeptId);

        dept.IsDeleted.Should().BeTrue();
        dept.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = CreateService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteAsync(Guid.NewGuid()));
    }
}

public class DepartmentValidatorTests
{
    private readonly CreateDepartmentRequestValidator _createValidator = new();
    private readonly UpdateDepartmentRequestValidator _updateValidator = new();

    [Fact]
    public void Create_ValidRequest_PassesValidation()
    {
        var result = _createValidator.TestValidate(
            new CreateDepartmentRequest("Finance", "Finance team"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Create_EmptyName_FailsValidation()
    {
        var result = _createValidator.TestValidate(
            new CreateDepartmentRequest("", "Finance team"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Create_TooLongName_FailsValidation()
    {
        var name = new string('A', 51);
        var result = _createValidator.TestValidate(
            new CreateDepartmentRequest(name, "Finance team"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Create_TooLongDescription_FailsValidation()
    {
        var description = new string('D', 251);
        var result = _createValidator.TestValidate(
            new CreateDepartmentRequest("Finance", description));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Update_NullFields_PassesValidation()
    {
        var result = _updateValidator.TestValidate(new UpdateDepartmentRequest(null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Update_TooLongName_FailsValidation()
    {
        var name = new string('A', 51);
        var result = _updateValidator.TestValidate(new UpdateDepartmentRequest(name, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Update_TooLongDescription_FailsValidation()
    {
        var description = new string('D', 251);
        var result = _updateValidator.TestValidate(new UpdateDepartmentRequest(null, description));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
