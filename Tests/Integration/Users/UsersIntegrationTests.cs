using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Users.Dtos;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Integration.Users;

public class UsersIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public UsersIntegrationTests(ApiFactory factory) => _factory = factory;

    // ── POST /users ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_ValidRequest_Returns201()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync("/users", new
        {
            name = "New Test User",
            email = $"newuser_{Guid.NewGuid():N}@example.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Response<UserDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Name.Should().Be("New Test User");
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync("/users", new
        {
            name = "Dup",
            email = "admin@example.com",   // already seeded
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUser_InvalidBody_Returns400()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync("/users", new
        {
            name = "",
            email = "bad",
            password = "123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_Unauthenticated_Returns401()
    {
        var client = _factory.CreateAnonClient();

        var response = await client.PostAsJsonAsync("/users", new
        {
            name = "X",
            email = "x@x.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateUser_InsufficientPermission_Returns403()
    {
        // Viewer only has read:User, not create:User
        var client = _factory.CreateViewerClient();

        var response = await client.PostAsJsonAsync("/users", new
        {
            name = "X",
            email = "x@x.com",
            password = "Password1!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /users ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsers_Admin_Returns200WithList()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<IEnumerable<UserDto>>>();
        body!.Data.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetAllUsers_Viewer_Returns200()
    {
        var client = _factory.CreateViewerClient();

        var response = await client.GetAsync("/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllUsers_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateAnonClient().GetAsync("/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /users/{id} ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserById_ValidId_Returns200()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync($"/users/{EntityBuilder.AdminUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<UserDto>>();
        body!.Data!.Email.Should().Be("admin@example.com");
    }

    [Fact]
    public async Task GetUserById_NotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /users/{id} ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateUser_ValidData_Returns200()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PatchAsJsonAsync(
            $"/users/{EntityBuilder.RegularUserId}",
            new { name = "Updated Regular" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<UserDto>>();
        body!.Data!.Name.Should().Be("Updated Regular");
    }

    [Fact]
    public async Task UpdateUser_NotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.PatchAsJsonAsync($"/users/{Guid.NewGuid()}", new { name = "X" });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /users/{id} ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUser_ExistingUser_Returns200()
    {
        var client = _factory.CreateAdminClient();

        // Create a throwaway user first
        var createResp = await client.PostAsJsonAsync("/users", new
        {
            name = "To Delete",
            email = $"todel_{Guid.NewGuid():N}@example.com",
            password = "Password1!"
        });
        var created = await createResp.Content.ReadFromJsonAsync<Response<UserDto>>();

        var response = await client.DeleteAsync($"/users/{created!.Data!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteUser_NotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.DeleteAsync($"/users/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_NoPermission_Returns403()
    {
        var client = _factory.CreateViewerClient();
        var response = await client.DeleteAsync($"/users/{EntityBuilder.RegularUserId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}