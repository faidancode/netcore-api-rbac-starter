using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Departments;
using netcore_api_rbac_starter.Modules.Departments.Dtos;
using netcore_api_rbac_starter.Modules.Departments.Validators;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Unit.Departments;

public class DepartmentsServiceTests
{
    [Fact]
    public async Task Create_ValidRequest_ReturnsDepartmentDto()
    {
        await using var db = DbContextFactory.Create();
        var svc = new DepartmentsService(db);

        var result = await svc.CreateAsync(new CreateDepartmentRequest("Finance", "Finance team"));

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Finance");
        result.Description.Should().Be("Finance team");
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new DepartmentsService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.CreateAsync(new CreateDepartmentRequest("Engineering", "Duplicate")));
    }

    [Fact]
    public async Task GetAll_ReturnsAllActiveDepartments()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new DepartmentsService(db);

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

        var svc = new DepartmentsService(db);
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
        var svc = new DepartmentsService(db);

        var result = await svc.GetByIdAsync(EntityBuilder.EngineeringId);

        result.Id.Should().Be(EntityBuilder.EngineeringId);
        result.Name.Should().Be("Engineering");
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = new DepartmentsService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ChangesNameAndDescription()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new DepartmentsService(db);

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
        var svc = new DepartmentsService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.UpdateAsync(EntityBuilder.EngineeringId,
                new UpdateDepartmentRequest("Human Resources", null)));
    }

    [Fact]
    public async Task Update_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = new DepartmentsService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.UpdateAsync(Guid.NewGuid(), new UpdateDepartmentRequest("X", null)));
    }

    [Fact]
    public async Task Delete_ExistingDepartment_SoftDeletesDepartment()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new DepartmentsService(db);

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
        var svc = new DepartmentsService(db);

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
