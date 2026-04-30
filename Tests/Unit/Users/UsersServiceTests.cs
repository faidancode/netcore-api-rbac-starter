using FluentAssertions;
using FluentValidation.TestHelper;
using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using Microsoft.EntityFrameworkCore;
using netcore_api_rbac_starter.Modules.Users;
using netcore_api_rbac_starter.Modules.Users.Dtos;
using netcore_api_rbac_starter.Modules.Users.Validators;
using netcore_api_rbac_starter.Tests.Helpers;
using netcore_api_rbac_starter.Security;

namespace netcore_api_rbac_starter.Tests.Unit.Users;

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public TestCurrentUserService(Guid userId, bool isAuthenticated)
    {
        UserId = userId;
        IsAuthenticated = isAuthenticated;
    }

    public Guid UserId { get; }
    public string Email => "admin@example.com";
    public bool IsAuthenticated { get; }
    public IEnumerable<string> Permissions => Enumerable.Empty<string>();

    public bool HasPermission(string action, string subject) => false;
}

public class UsersServiceTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_ReturnsUserDto()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new UsersService(db);

        var result = await svc.CreateAsync(new CreateUserRequest(
            "New Person", "newperson@example.com", "Password1!", EntityBuilder.AdminRoleId));

        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("New Person");
        result.Email.Should().Be("newperson@example.com");
        result.RoleId.Should().Be(EntityBuilder.AdminRoleId);
        result.RoleName.Should().Be("Admin");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_DuplicateEmail_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new UsersService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.CreateAsync(new CreateUserRequest(
                "Duplicate", "admin@example.com", "Password1!", null)));
    }

    [Fact]
    public async Task Create_NonExistentRole_ThrowsNotFound()
    {
        await using var db = DbContextFactory.Create();
        var svc = new UsersService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.CreateAsync(new CreateUserRequest(
                "User", "u@example.com", "Password1!", Guid.NewGuid())));
    }

    [Fact]
    public async Task Create_PasswordIsHashed()
    {
        await using var db = DbContextFactory.Create();
        var svc = new UsersService(db);

        await svc.CreateAsync(new CreateUserRequest("Hash Test", "hash@example.com", "MySecret!", null));

        var user = db.Users.First(u => u.Email == "hash@example.com");
        user.PasswordHash.Should().NotBe("MySecret!");
        BCrypt.Net.BCrypt.Verify("MySecret!", user.PasswordHash).Should().BeTrue();
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllActiveUsers()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        var svc = new UsersService(db);
        var pagedResult = await svc.GetAllAsync(new ListUsersQuery());
        var result = pagedResult.Items.ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Email == "admin@example.com");
        result.Should().Contain(u => u.Email == "user@example.com");
    }

    [Fact]
    public async Task GetAll_DoesNotReturnSoftDeletedUsers()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        var user = db.Users.First(u => u.Email == "user@example.com");
        user.IsDeleted = true;
        await db.SaveChangesAsync();

        var svc = new UsersService(db);
        var pagedResult = await svc.GetAllAsync(new ListUsersQuery());
        var result = pagedResult.Items.ToList();

        result.Should().HaveCount(1);
        result.Should().NotContain(u => u.Email == "user@example.com");
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ValidId_ReturnsUser()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new UsersService(db);

        var result = await svc.GetByIdAsync(EntityBuilder.AdminUserId);

        result.Id.Should().Be(EntityBuilder.AdminUserId);
        result.RoleName.Should().Be("Admin");
    }

    [Fact]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        await using var db = DbContextFactory.Create();
        var svc = new UsersService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetByIdAsync(Guid.NewGuid()));
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ChangeName_UpdatesField()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new UsersService(db);

        var result = await svc.UpdateAsync(EntityBuilder.AdminUserId,
            new UpdateUserRequest("Updated Name", null, null, null));

        result.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Update_DuplicateEmail_ThrowsConflict()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new UsersService(db);

        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.UpdateAsync(EntityBuilder.AdminUserId,
                new UpdateUserRequest(null, "user@example.com", null, null)));
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_HashesNewPassword()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new UsersService(db);

        await svc.ChangePasswordAsync(EntityBuilder.AdminUserId,
            new ChangeUserPasswordRequest
            {
                CurrentPassword = null,
                NewPassword = "NewPass@456!",
                ConfirmPassword = "NewPass@456!"
            });

        var user = db.Users.First(u => u.Id == EntityBuilder.AdminUserId);
        BCrypt.Net.BCrypt.Verify("NewPass@456!", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_SelfServiceWrongCurrentPassword_ThrowsUnauthorized()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var currentUser = new TestCurrentUserService(EntityBuilder.AdminUserId, true);
        var svc = new UsersService(db, currentUser);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            svc.ChangePasswordAsync(EntityBuilder.AdminUserId,
                new ChangeUserPasswordRequest
                {
                    CurrentPassword = "WrongPassword",
                    NewPassword = "NewPass@456!",
                    ConfirmPassword = "NewPass@456!"
                }));
    }

    [Fact]
    public async Task Update_NonExistentUser_ThrowsNotFound()
    {
        await using var db = DbContextFactory.Create();
        var svc = new UsersService(db);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.UpdateAsync(Guid.NewGuid(), new UpdateUserRequest("X", null, null, null)));
    }

    [Fact]
    public async Task Update_SetRoleToEmpty_ClearsRole()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new UsersService(db);

        var result = await svc.UpdateAsync(EntityBuilder.AdminUserId,
            new UpdateUserRequest(null, null, Guid.Empty, null));

        result.RoleId.Should().BeNull();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingUser_SoftDeletesUser()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new UsersService(db);

        await svc.DeleteAsync(EntityBuilder.RegularUserId);

        var user = await db.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Id == EntityBuilder.RegularUserId, CancellationToken.None);
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_NonExistentUser_ThrowsNotFound()
    {
        await using var db = DbContextFactory.Create();
        var svc = new UsersService(db);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_SoftDeleted_NotReturnedInGetAll()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new UsersService(db);

        await svc.DeleteAsync(EntityBuilder.RegularUserId);

        var pagedResult = await svc.GetAllAsync(new ListUsersQuery());
        var result = pagedResult.Items.ToList();
        result.Should().NotContain(u => u.Id == EntityBuilder.RegularUserId);
    }
}

// ── Validator Tests ────────────────────────────────────────────────────────────
public class UserValidatorTests
{
    private readonly CreateUserRequestValidator _createValidator = new();
    private readonly UpdateUserRequestValidator _updateValidator = new();

    [Fact]
    public void Create_ValidRequest_PassesValidation()
    {
        var result = _createValidator.TestValidate(
            new CreateUserRequest("Alice", "alice@example.com", "Password1!", null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    public void Create_EmptyName_FailsValidation(string name)
    {
        var result = _createValidator.TestValidate(
            new CreateUserRequest(name, "alice@example.com", "Password1!", null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Create_TooLongName_FailsValidation()
    {
        var name = new string('A', 101);
        var result = _createValidator.TestValidate(
            new CreateUserRequest(name, "alice@example.com", "Password1!", null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Create_InvalidEmail_FailsValidation()
    {
        var result = _createValidator.TestValidate(
            new CreateUserRequest("Alice", "not-an-email", "Password1!", null));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Create_ShortPassword_FailsValidation()
    {
        var result = _createValidator.TestValidate(
            new CreateUserRequest("Alice", "alice@example.com", "abc", null));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Update_NullFields_PassesValidation()
    {
        // All fields nullable — empty update is valid
        var result = _updateValidator.TestValidate(
            new UpdateUserRequest(null, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Update_InvalidEmail_FailsValidation()
    {
        var result = _updateValidator.TestValidate(
            new UpdateUserRequest(null, "bad-email", null, null));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ChangePassword_ValidRequest_PassesValidation()
    {
        var validator = new ChangeUserPasswordRequestValidator();
        var result = validator.TestValidate(
            new ChangeUserPasswordRequest
            {
                CurrentPassword = "Current1!",
                NewPassword = "Password1!",
                ConfirmPassword = "Password1!"
            });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ChangePassword_MismatchConfirm_FailsValidation()
    {
        var validator = new ChangeUserPasswordRequestValidator();
        var result = validator.TestValidate(
            new ChangeUserPasswordRequest
            {
                CurrentPassword = "Current1!",
                NewPassword = "Password1!",
                ConfirmPassword = "Password2!"
            });
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }
}
