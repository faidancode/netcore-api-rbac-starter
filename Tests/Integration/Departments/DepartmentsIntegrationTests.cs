using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Departments.Dtos;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Integration.Departments;

public class DepartmentsIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public DepartmentsIntegrationTests(ApiFactory factory) => _factory = factory;

    // ── POST /departments ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDepartment_ValidRequest_Returns201()
    {
        var client = _factory.CreateAdminClient();
        var name = $"Finance_{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/departments", new { name, description = "Financial" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Response<DepartmentDto>>();
        body!.Data!.Name.Should().Be(name);
    }

    [Fact]
    public async Task CreateDepartment_DuplicateName_Returns409()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync("/departments", new { name = "Engineering" });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateDepartment_EmptyName_Returns400()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync("/departments", new { name = "" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDepartment_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateAnonClient()
            .PostAsJsonAsync("/departments", new { name = "X" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDepartment_ViewerRole_Returns403()
    {
        var response = await _factory.CreateViewerClient()
            .PostAsJsonAsync("/departments", new { name = "X" });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /departments ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllDepartments_Returns200WithList()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/departments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<IEnumerable<DepartmentDto>>>();
        body!.Data.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── GET /departments/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetDepartmentById_ValidId_Returns200()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/departments/{EntityBuilder.EngineeringId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<DepartmentDto>>();
        body!.Data!.Name.Should().Be("Engineering");
    }

    [Fact]
    public async Task GetDepartmentById_NotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/departments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /departments/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateDepartment_ValidData_Returns200()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PatchAsJsonAsync(
            $"/departments/{EntityBuilder.HrDeptId}",
            new { description = "Updated HR" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<DepartmentDto>>();
        body!.Data!.Description.Should().Be("Updated HR");
    }

    [Fact]
    public async Task UpdateDepartment_DuplicateName_Returns409()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PatchAsJsonAsync(
            $"/departments/{EntityBuilder.EngineeringId}",
            new { name = "Human Resources" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── DELETE /departments/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteDepartment_ExistingDepartment_Returns200()
    {
        var client = _factory.CreateAdminClient();

        var createResp = await client.PostAsJsonAsync("/departments",
            new { name = $"ToDelete_{Guid.NewGuid():N}" });
        var created = await createResp.Content.ReadFromJsonAsync<Response<DepartmentDto>>();

        var response = await client.DeleteAsync($"/departments/{created!.Data!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteDepartment_NotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.DeleteAsync($"/departments/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}