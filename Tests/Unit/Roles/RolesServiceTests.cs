using FluentAssertions;
using FluentValidation.TestHelper;
using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Roles;
using netcore_api_rbac_starter.Modules.Roles.Dtos;
using netcore_api_rbac_starter.Modules.Roles.Validators;
using netcore_api_rbac_starter.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter.Tests.Unit.Roles;

public class RolesServiceTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRole_ReturnsRoleDto()
    {
        await using var db = DbContextFactory.Create();
        var svc = new RolesService(db);

        var result = await svc.CreateAsync(new CreateRoleRequest("Editor", "Can edit content"));

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Editor");
        result.Description.Should().Be("Can edit content");
        result.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.CreateAsync(new CreateRoleRequest("Admin", null)));
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllActiveRoles()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        var result = (await svc.GetAllAsync()).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "Admin");
        result.Should().Contain(r => r.Name == "Viewer");
    }

    [Fact]
    public async Task GetAll_IncludesPermissionsForEachRole()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        var roles = (await svc.GetAllAsync()).ToList();
        var admin = roles.Single(r => r.Name == "Admin");

        admin.Permissions.Should().ContainSingle(p => p.Action == "manage" && p.Subject == "all");
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ValidId_ReturnsRole()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        var result = await svc.GetByIdAsync(EntityBuilder.AdminRoleId);

        result.Id.Should().Be(EntityBuilder.AdminRoleId);
        result.Name.Should().Be("Admin");
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = new RolesService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidData_UpdatesRole()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        var result = await svc.UpdateAsync(EntityBuilder.ViewerRoleId,
            new UpdateRoleRequest("SuperViewer", "Updated description"));

        result.Name.Should().Be("SuperViewer");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task Update_DuplicateName_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.UpdateAsync(EntityBuilder.ViewerRoleId, new UpdateRoleRequest("Admin", null)));
    }

    [Fact]
    public async Task Update_SameNameNoChange_DoesNotThrow()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        // Updating a role to its own name should not throw a conflict
        var result = await svc.UpdateAsync(EntityBuilder.AdminRoleId,
            new UpdateRoleRequest("Admin", "New description"));

        result.Name.Should().Be("Admin");
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingRole_SoftDeletes()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        await svc.DeleteAsync(EntityBuilder.ViewerRoleId);

        var role = await db.Roles.IgnoreQueryFilters()
            .FirstAsync(r => r.Id == EntityBuilder.ViewerRoleId);
        role.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = new RolesService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteAsync(Guid.NewGuid()));
    }

    // ── AssignPermissions ────────────────────────────────────────────────────

    [Fact]
    public async Task AssignPermissions_ClearsAndReplacesExisting()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        // Add a second permission to assign
        var readPerm = new Permission { Action = "read", Subject = "Employee" };
        db.Permissions.Add(readPerm);
        await db.SaveChangesAsync();

        var svc = new RolesService(db);

        // Admin previously had manage:all; now replace with read:Employee only
        var result = await svc.AssignPermissionsAsync(EntityBuilder.AdminRoleId,
            new AssignPermissionsRequest([readPerm.Id]));

        result.Permissions.Should().HaveCount(1);
        result.Permissions.Should().ContainSingle(p => p.Action == "read" && p.Subject == "Employee");
    }

    [Fact]
    public async Task AssignPermissions_EmptyList_ClearsAllPermissions()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        var result = await svc.AssignPermissionsAsync(EntityBuilder.AdminRoleId,
            new AssignPermissionsRequest([]));

        result.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignPermissions_UnknownPermissionId_ThrowsNotFound()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.AssignPermissionsAsync(EntityBuilder.AdminRoleId,
                new AssignPermissionsRequest([Guid.NewGuid()])));
    }

    [Fact]
    public async Task AssignPermissions_UnknownRoleId_ThrowsNotFound()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new RolesService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.AssignPermissionsAsync(Guid.NewGuid(), new AssignPermissionsRequest([])));
    }
}

public class RoleValidatorTests
{
    private readonly CreateRoleRequestValidator _createValidator = new();
    private readonly AssignPermissionsRequestValidator _assignValidator = new();

    [Fact]
    public void Create_ValidRequest_PassesValidation()
    {
        var result = _createValidator.TestValidate(new CreateRoleRequest("Manager", null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Create_EmptyName_FailsValidation()
    {
        var result = _createValidator.TestValidate(new CreateRoleRequest("", null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void AssignPermissions_NullIds_FailsValidation()
    {
        var result = _assignValidator.TestValidate(new AssignPermissionsRequest(null!));
        result.ShouldHaveValidationErrorFor(x => x.PermissionIds);
    }

    [Fact]
    public void AssignPermissions_EmptyList_PassesValidation()
    {
        var result = _assignValidator.TestValidate(new AssignPermissionsRequest([]));
        result.ShouldNotHaveAnyValidationErrors();
    }
}