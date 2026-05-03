using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Auth.Dtos;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Integration.Auth;

public class AuthIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthIntegrationTests(ApiFactory factory) => _factory = factory;

    // ── POST /api/v1/auth/login ──────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        var client = _factory.CreateAnonClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "admin@example.com", password = "Admin@123" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<Response<LoginResponse>>();
        body!.Success.Should().BeTrue();
        body.Data!.AccessToken.Should().NotBeNullOrEmpty();
        body.Data.RefreshToken.Should().NotBeNullOrEmpty();
        body.Data.User.Email.Should().Be("admin@example.com");
        body.Data.User.RoleName.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateAnonClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "admin@example.com", password = "WRONG" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateAnonClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "ghost@example.com", password = "Admin@123" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_InvalidRequestBody_Returns400()
    {
        var client = _factory.CreateAnonClient();

        // Missing password, invalid email
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "not-an-email", password = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/v1/auth/refresh ────────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        var client = _factory.CreateAnonClient();

        // First login to obtain a refresh token
        var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "admin@example.com", password = "Admin@123" });
        var loginBody = await loginResp.Content.ReadFromJsonAsync<Response<LoginResponse>>();
        var refreshToken = loginBody!.Data!.RefreshToken;

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<LoginResponse>>();
        body!.Data!.AccessToken.Should().NotBeNullOrEmpty();
        body.Data.RefreshToken.Should().NotBeNullOrEmpty();
        // Tokens should rotate
        body.Data.RefreshToken.Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401()
    {
        var client = _factory.CreateAnonClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { refreshToken = "fake-token-that-does-not-exist" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── POST /api/v1/auth/me ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_Authenticated_Returns200WithUserInfo()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsync("/api/v1/auth/me", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<MeResponse>>();
        body!.Data!.Id.Should().Be(EntityBuilder.AdminUserId);
        body.Data.Email.Should().Be("admin@example.com");
    }

    [Fact]
    public async Task Me_Unauthenticated_Returns401()
    {
        var client = _factory.CreateAnonClient();

        var response = await client.PostAsync("/api/v1/auth/me", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /api/v1/auth/me/permissions ──────────────────────────────────────────────

    [Fact]
    public async Task GetMyPermissions_Authenticated_ReturnsPermissions()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/api/v1/auth/me/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<IEnumerable<PermissionDto>>>();
        body!.Data.Should().ContainSingle(p => p.Action == "manage" && p.Subject == "all");
    }

    [Fact]
    public async Task GetMyPermissions_Unauthenticated_Returns401()
    {
        var client = _factory.CreateAnonClient();

        var response = await client.GetAsync("/api/v1/auth/me/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}