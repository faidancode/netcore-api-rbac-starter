using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Security;
using Microsoft.Extensions.Configuration;

namespace netcore_api_rbac_starter.Tests.Unit.Auth;

public class JwtServiceTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "super-secret-key-that-is-32-chars-min!",
            ["Jwt:Issuer"] = "TestIssuer",
            ["Jwt:Audience"] = "TestAudience",
            ["Jwt:AccessTokenExpiryMinutes"] = "60",
            ["Jwt:RefreshTokenExpiryDays"] = "7"
        }).Build();

    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test User",
        Email = "test@example.com",
        PasswordHash = "hash",
        RoleId = Guid.NewGuid()
    };

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt()
    {
        var svc = new JwtService(BuildConfig());
        var user = MakeUser();
        var perms = new List<Permission>
        {
            new() { Action = "read", Subject = "Employee" }
        };

        var token = svc.GenerateAccessToken(user, perms);

        token.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();

        var jwt = handler.ReadJwtToken(token);
        jwt.Issuer.Should().Be("TestIssuer");
        jwt.Claims.Should().Contain(c => c.Type == "permission" && c.Value == "read:Employee");
    }

    [Fact]
    public void GenerateAccessToken_EmbedsManyPermissions()
    {
        var svc = new JwtService(BuildConfig());
        var user = MakeUser();
        var perms = new List<Permission>
        {
            new() { Action = "manage", Subject = "all" },
            new() { Action = "read",   Subject = "User" },
            new() { Action = "create", Subject = "Employee" }
        };

        var token = svc.GenerateAccessToken(user, perms);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var permClaims = jwt.Claims.Where(c => c.Type == "permission").Select(c => c.Value).ToList();
        permClaims.Should().Contain("manage:all");
        permClaims.Should().Contain("read:User");
        permClaims.Should().Contain("create:Employee");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyBase64String()
    {
        var svc = new JwtService(BuildConfig());
        var token = svc.GenerateRefreshToken();
        var token2 = svc.GenerateRefreshToken();

        token.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token.Should().NotBe(token2, "refresh tokens must be unique");

        // Should be valid Base64
        var bytes = Convert.FromBase64String(token);
        bytes.Length.Should().Be(64);
    }

    [Fact]
    public void GetUserIdFromToken_ValidToken_ReturnsUserId()
    {
        var svc = new JwtService(BuildConfig());
        var user = MakeUser();

        var token = svc.GenerateAccessToken(user, Enumerable.Empty<Permission>());
        var userId = svc.GetUserIdFromToken(token);

        userId.Should().Be(user.Id);
    }

    [Fact]
    public void GetUserIdFromToken_InvalidToken_ReturnsNull()
    {
        var svc = new JwtService(BuildConfig());
        svc.GetUserIdFromToken("not.a.valid.token").Should().BeNull();
    }

    [Fact]
    public void AccessToken_ContainsUserEmailClaim()
    {
        var svc = new JwtService(BuildConfig());
        var user = MakeUser();

        var token = svc.GenerateAccessToken(user, Enumerable.Empty<Permission>());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Email && c.Value == user.Email);
    }
}