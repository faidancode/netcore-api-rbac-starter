using FluentAssertions;
using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Auth;
using netcore_api_rbac_starter.Modules.Auth.Dtos;
using netcore_api_rbac_starter.Security;
using netcore_api_rbac_starter.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace netcore_api_rbac_starter.Tests.Unit.Auth;

public class AuthServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────
    private static IConfiguration BuildConfig(int accessMinutes = 60, int refreshDays = 7)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "test-secret-key-at-least-32-characters-long!",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:AccessTokenExpiryMinutes"] = accessMinutes.ToString(),
            ["Jwt:RefreshTokenExpiryDays"] = refreshDays.ToString()
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static Mock<IJwtService> BuildJwtMock(
        string accessToken = "access-token",
        string refreshToken = "refresh-token")
    {
        var mock = new Mock<IJwtService>();
        mock.Setup(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<Permission>>()))
            .Returns(accessToken);
        mock.Setup(j => j.GenerateRefreshToken())
            .Returns(refreshToken);
        return mock;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Login
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var jwtMock = BuildJwtMock();
        var svc = new AuthService(db, jwtMock.Object, BuildConfig());

        // Act
        var result = await svc.LoginAsync(new LoginRequest("admin@example.com", "Admin@123"));

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Email.Should().Be("admin@example.com");
        result.User.RoleName.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.LoginAsync(new LoginRequest("admin@example.com", "wrong-password")));
    }

    [Fact]
    public async Task Login_UnknownEmail_ThrowsUnauthorized()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.LoginAsync(new LoginRequest("nobody@example.com", "pass")));
    }

    [Fact]
    public async Task Login_InactiveUser_ThrowsUnauthorized()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        var user = await db.Users.FindAsync(EntityBuilder.AdminUserId);
        user!.IsActive = false;
        await db.SaveChangesAsync();

        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.LoginAsync(new LoginRequest("admin@example.com", "Admin@123")));
    }

    [Fact]
    public async Task Login_PersistsRefreshTokenToDatabase()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        await svc.LoginAsync(new LoginRequest("admin@example.com", "Admin@123"));

        var stored = await db.RefreshTokens.FirstOrDefaultAsync();
        stored.Should().NotBeNull();
        stored!.Token.Should().Be("refresh-token");
        stored.IsRevoked.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Refresh
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = "valid-refresh-token",
            UserId = EntityBuilder.AdminUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        var jwtMock = BuildJwtMock("new-access", "new-refresh");
        var svc = new AuthService(db, jwtMock.Object, BuildConfig());

        var result = await svc.RefreshAsync(new RefreshRequest("valid-refresh-token"));

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("new-refresh");
    }

    [Fact]
    public async Task Refresh_RevokesOldToken()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = "old-token",
            UserId = EntityBuilder.AdminUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());
        await svc.RefreshAsync(new RefreshRequest("old-token"));

        var old = await db.RefreshTokens.FirstAsync(t => t.Token == "old-token");
        old.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_ExpiredToken_ThrowsUnauthorized()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = "expired-token",
            UserId = EntityBuilder.AdminUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)   // already expired
        });
        await db.SaveChangesAsync();

        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.RefreshAsync(new RefreshRequest("expired-token")));
    }

    [Fact]
    public async Task Refresh_RevokedToken_ThrowsUnauthorized()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = "revoked-token",
            UserId = EntityBuilder.AdminUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true
        });
        await db.SaveChangesAsync();

        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.RefreshAsync(new RefreshRequest("revoked-token")));
    }

    [Fact]
    public async Task Refresh_UnknownToken_ThrowsUnauthorized()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => svc.RefreshAsync(new RefreshRequest("does-not-exist")));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Me / Permissions
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMe_ValidUserId_ReturnsUserWithPermissions()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        var result = await svc.GetMeAsync(EntityBuilder.AdminUserId);

        result.Id.Should().Be(EntityBuilder.AdminUserId);
        result.Email.Should().Be("admin@example.com");
        result.RoleName.Should().Be("Admin");
        result.Permissions.Should().ContainSingle(p => p.Action == "manage" && p.Subject == "all");
    }

    [Fact]
    public async Task GetMe_UnknownUserId_ThrowsNotFound()
    {
        await using var db = DbContextFactory.Create();
        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        await Assert.ThrowsAsync<NotFoundException>(
            () => svc.GetMeAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetMyPermissions_ReturnsPermissionsForRole()
    {
        await using var db = DbContextFactory.Create();
        await EntityBuilder.SeedDefaultDataAsync(db);
        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        var perms = await svc.GetMyPermissionsAsync(EntityBuilder.AdminUserId);

        perms.Should().ContainSingle(p => p.Action == "manage" && p.Subject == "all");
    }

    [Fact]
    public async Task GetMyPermissions_UserWithNoRole_ReturnsEmpty()
    {
        await using var db = DbContextFactory.Create();
        // User with no role
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Name = "Roleless",
            Email = "roleless@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("pass"),
            RoleId = null
        });
        await db.SaveChangesAsync();

        var rolelessUser = await db.Users.FirstAsync(u => u.Email == "roleless@example.com");
        var svc = new AuthService(db, BuildJwtMock().Object, BuildConfig());

        var perms = await svc.GetMyPermissionsAsync(rolelessUser.Id);

        perms.Should().BeEmpty();
    }
}