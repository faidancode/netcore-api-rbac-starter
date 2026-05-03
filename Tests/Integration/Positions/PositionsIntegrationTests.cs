using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Positions.Dtos;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Integration.Positions;

public class PositionsIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public PositionsIntegrationTests(ApiFactory factory) => _factory = factory;

    // ── POST /positions ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePosition_ValidRequest_Returns201()
    {
        var client = _factory.CreateAdminClient();
        var name = $"Finance_{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/v1/positions", new { name, description = "Financial", departmentId = EntityBuilder.EngineeringId });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Response<PositionDto>>();
        body!.Data!.Name.Should().Be(name);
        body.Data.DepartmentId.Should().Be(EntityBuilder.EngineeringId);
    }

    [Fact]
    public async Task CreatePosition_DuplicateName_Returns409()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync("/api/v1/positions", new { name = "Senior Developer", departmentId = EntityBuilder.EngineeringId });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatePosition_EmptyName_Returns400()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.PostAsJsonAsync("/api/v1/positions", new { name = "", departmentId = EntityBuilder.EngineeringId });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePosition_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateAnonClient()
            .PostAsJsonAsync("/api/v1/positions", new { name = "ValidName", departmentId = EntityBuilder.EngineeringId });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePosition_ViewerRole_Returns403()
    {
        var response = await _factory.CreateViewerClient()
            .PostAsJsonAsync("/api/v1/positions", new { name = "ValidName", departmentId = EntityBuilder.EngineeringId });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /positions ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPositions_Returns200WithList()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/v1/positions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<IEnumerable<PositionDto>>>();
        body!.Data.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── GET /positions/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetPositionById_ValidId_Returns200()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/api/v1/positions/{EntityBuilder.SeniorDevId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<PositionDto>>();
        body!.Data!.Name.Should().Be("Senior Developer");
    }

    [Fact]
    public async Task GetPositionById_NotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/api/v1/positions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /positions/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task UpdatePosition_ValidData_Returns200()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/positions/{EntityBuilder.HrManagerId}",
            new { description = "Updated HR", departmentId = EntityBuilder.EngineeringId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<PositionDto>>();
        body!.Data!.Description.Should().Be("Updated HR");
        body.Data.DepartmentId.Should().Be(EntityBuilder.EngineeringId);
    }

    [Fact]
    public async Task UpdatePosition_DuplicateName_Returns409()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/positions/{EntityBuilder.SeniorDevId}",
            new { name = "HR Manager", departmentId = EntityBuilder.HrDeptId });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── DELETE /positions/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task DeletePosition_ExistingPosition_Returns200()
    {
        var client = _factory.CreateAdminClient();

        var createResp = await client.PostAsJsonAsync("/api/v1/positions",
            new { name = $"ToDelete_{Guid.NewGuid():N}", departmentId = EntityBuilder.EngineeringId });
        var created = await createResp.Content.ReadFromJsonAsync<Response<PositionDto>>();

        var response = await client.DeleteAsync($"/api/v1/positions/{created!.Data!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeletePosition_NotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.DeleteAsync($"/api/v1/positions/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
