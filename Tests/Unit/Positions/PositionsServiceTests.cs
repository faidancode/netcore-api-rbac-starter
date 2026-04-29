using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Positions;
using netcore_api_rbac_starter.Modules.Positions.Dtos;
using netcore_api_rbac_starter.Modules.Positions.Validators;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Unit.Positions;

public class PositionsServiceTests
{
    [Fact]
    public async Task Create_ValidRequest_ReturnsPositionDto()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new PositionsService(db);

        var result = await svc.CreateAsync(new CreatePositionRequest("Staff", "Staff position", EntityBuilder.EngineeringId));

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Staff");
        result.Description.Should().Be("Staff position");
        result.DepartmentId.Should().Be(EntityBuilder.EngineeringId);
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new PositionsService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.CreateAsync(new CreatePositionRequest("Senior Developer", "Duplicate", EntityBuilder.EngineeringId)));
    }

    [Fact]
    public async Task GetAll_ReturnsAllActivePositions()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new PositionsService(db);

        var pagedResult = await svc.GetAllAsync(new ListPositionQuery());
        var result = pagedResult.Items.ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Name == "Senior Developer");
        result.Should().Contain(d => d.Name == "HR Manager");
        result.Should().OnlyContain(d => !string.IsNullOrWhiteSpace(d.DepartmentName));
    }

    [Fact]
    public async Task GetAll_ExcludesSoftDeletedPositions()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        var pos = await db.Positions.FirstAsync(d => d.Id == EntityBuilder.SeniorDevId);
        pos.IsDeleted = true;
        await db.SaveChangesAsync();

        var svc = new PositionsService(db);
        var pagedResult = await svc.GetAllAsync(new ListPositionQuery());
        var result = pagedResult.Items.ToList();

        result.Should().HaveCount(1);
        result.Should().NotContain(d => d.Name == "Senior Developer");
    }

    [Fact]
    public async Task GetById_ValidId_ReturnsPosition()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new PositionsService(db);

        var result = await svc.GetByIdAsync(EntityBuilder.SeniorDevId);

        result.Id.Should().Be(EntityBuilder.SeniorDevId);
        result.Name.Should().Be("Senior Developer");
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = new PositionsService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Update_ChangesNameAndDescription()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new PositionsService(db);

        var result = await svc.UpdateAsync(EntityBuilder.SeniorDevId,
            new UpdatePositionRequest("Lead Developer", "Lead position", null));

        result.Name.Should().Be("Lead Developer");
        result.Description.Should().Be("Lead position");
    }

    [Fact]
    public async Task Update_DuplicateName_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new PositionsService(db);

        // Try to update Senior Developer to HR Manager in the Engineering department (won't conflict since different depts)
        // Actually, let's seed another position in Engineering to test conflict.
        db.Positions.Add(new Position { Id = Guid.NewGuid(), Name = "Junior Developer", DepartmentId = EntityBuilder.EngineeringId });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.UpdateAsync(EntityBuilder.SeniorDevId,
                new UpdatePositionRequest("Junior Developer", null, null)));
    }

    [Fact]
    public async Task Update_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = new PositionsService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.UpdateAsync(Guid.NewGuid(), new UpdatePositionRequest("X", null, null)));
    }

    [Fact]
    public async Task Delete_ExistingPosition_SoftDeletesPosition()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new PositionsService(db);

        await svc.DeleteAsync(EntityBuilder.SeniorDevId);

        var pos = await db.Positions.IgnoreQueryFilters()
            .FirstAsync(d => d.Id == EntityBuilder.SeniorDevId);

        pos.IsDeleted.Should().BeTrue();
        pos.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = new PositionsService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteAsync(Guid.NewGuid()));
    }
}

public class PositionValidatorTests
{
    private readonly CreatePositionRequestValidator _createValidator = new();
    private readonly UpdatePositionRequestValidator _updateValidator = new();

    [Fact]
    public void Create_ValidRequest_PassesValidation()
    {
        var result = _createValidator.TestValidate(
            new CreatePositionRequest("Staff", "Staff position", Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Create_EmptyName_FailsValidation()
    {
        var result = _createValidator.TestValidate(
            new CreatePositionRequest("", "Staff position", Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Create_EmptyDepartmentId_FailsValidation()
    {
        var result = _createValidator.TestValidate(
            new CreatePositionRequest("Staff", "Staff position", Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.DepartmentId);
    }

    [Fact]
    public void Create_TooLongName_FailsValidation()
    {
        var name = new string('A', 201);
        var result = _createValidator.TestValidate(
            new CreatePositionRequest(name, "Staff position", Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Create_TooLongDescription_FailsValidation()
    {
        var description = new string('D', 501);
        var result = _createValidator.TestValidate(
            new CreatePositionRequest("Staff", description, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Update_NullFields_PassesValidation()
    {
        var result = _updateValidator.TestValidate(new UpdatePositionRequest(null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Update_TooLongName_FailsValidation()
    {
        var name = new string('A', 201);
        var result = _updateValidator.TestValidate(new UpdatePositionRequest(name, null, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Update_TooLongDescription_FailsValidation()
    {
        var description = new string('D', 501);
        var result = _updateValidator.TestValidate(new UpdatePositionRequest(null, description, null));
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
