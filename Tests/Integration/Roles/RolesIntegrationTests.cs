using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Auth.Dtos;
using netcore_api_rbac_starter.Modules.Roles.Dtos;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Integration.Roles;

public class RolesIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public RolesIntegrationTests(ApiFactory factory) => _factory = factory;

    // ── POST /roles ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRole_ValidRequest_Returns201()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync("/roles",
            new
            {
                name = $"TestRole_{Guid.NewGuid():N}",
                description = "Test",
                permissionIds = new[] { EntityBuilder.ReadEmployeePermId, EntityBuilder.ReadDepartmentPermId }
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Response<RoleDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.Permissions.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateRole_DuplicateName_Returns409()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync("/roles", new { name = "Admin", permissionIds = Array.Empty<Guid>() });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateRole_EmptyName_Returns400()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync("/roles", new { name = "", permissionIds = Array.Empty<Guid>() });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRole_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateAnonClient().PostAsJsonAsync(
            "/roles",
            new { name = "ValidRole", permissionIds = Array.Empty<Guid>() });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GET /roles ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRoles_Returns200WithRoles()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<IEnumerable<RoleDto>>>();
        body!.Data.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetPermissions_Returns200WithMasterPermissions()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/roles/permissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<IEnumerable<PermissionDto>>>();
        body!.Data.Should().Contain(p => p.Subject == "Department" && p.Action == "read");
        body.Data.Should().Contain(p => p.Subject == "Employee" && p.Action == "create");
    }

    // ── GET /roles/{id} ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoleById_ValidId_Returns200()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/roles/{EntityBuilder.AdminRoleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<RoleDto>>();
        body!.Data!.Name.Should().Be("Admin");
        body.Data.Permissions.Should().ContainSingle(p => p.Action == "manage");
    }

    [Fact]
    public async Task GetRoleById_NotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/roles/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /roles/{id} ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRole_ValidData_Returns200()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.PatchAsJsonAsync(
            $"/roles/{EntityBuilder.ViewerRoleId}",
            new
            {
                description = "Read only access",
                permissionIds = new[] { EntityBuilder.ReadDepartmentPermId, EntityBuilder.ReadEmployeePermId }
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<RoleDto>>();
        body!.Data!.Description.Should().Be("Read only access");
        body.Data.Permissions.Should().HaveCount(2);
    }

    // ── DELETE /roles/{id} ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRole_ExistingRole_Returns200()
    {
        var client = _factory.CreateAdminClient();

        // Create a role to delete
        var createResp = await client.PostAsJsonAsync("/roles",
            new { name = $"ToDelete_{Guid.NewGuid():N}", permissionIds = Array.Empty<Guid>() });
        var created = await createResp.Content.ReadFromJsonAsync<Response<RoleDto>>();

        var response = await client.DeleteAsync($"/roles/{created!.Data!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── POST /roles/{id}/permissions ─────────────────────────────────────────

    [Fact]
    public async Task AssignPermissions_ValidRequest_Returns200WithUpdatedPermissions()
    {
        var client = _factory.CreateAdminClient();

        // Viewer starts with no permissions — assign manage:all
        var response = await client.PostAsJsonAsync(
            $"/roles/{EntityBuilder.ViewerRoleId}/permissions",
            new { permissionIds = new[] { EntityBuilder.ManageAllPermId } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<RoleDto>>();
        body!.Data!.Permissions.Should().ContainSingle(p => p.Action == "manage");
    }

    [Fact]
    public async Task AssignPermissions_EmptyList_ClearsPermissions()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            $"/roles/{EntityBuilder.ViewerRoleId}/permissions",
            new { permissionIds = Array.Empty<Guid>() });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<RoleDto>>();
        body!.Data!.Permissions.Should().BeEmpty();
    }

    [Fact]
    public async Task AssignPermissions_UnknownPermissionId_Returns404()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            $"/roles/{EntityBuilder.AdminRoleId}/permissions",
            new { permissionIds = new[] { Guid.NewGuid() } });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
