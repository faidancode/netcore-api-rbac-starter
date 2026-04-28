using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Employees.Dtos;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Integration.Employees;

public class EmployeesIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EmployeesIntegrationTests(ApiFactory factory) => _factory = factory;

    // ── POST /employees ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateEmployee_ValidRequest_Returns201()
    {
        var client = _factory.CreateAdminClient();
        var nip = $"EMP-{Guid.NewGuid():N}";
        
        var req = new CreateEmployeeRequest(
            FullName: "Integration Test Employee",
            Nip: nip,
            Gender: Gender.Male,
            PositionId: EntityBuilder.SeniorDevId,
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: new DateOnly(2023, 1, 1),
            DepartmentId: EntityBuilder.EngineeringId
        );

        var response = await client.PostAsJsonAsync("/employees", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<Response<EmployeeDto>>();
        body!.Data!.FullName.Should().Be("Integration Test Employee");
        body.Data.Nip.Should().Be(nip);
    }

    [Fact]
    public async Task CreateEmployee_DuplicateNip_Returns409()
    {
        var client = _factory.CreateAdminClient();
        
        var req = new CreateEmployeeRequest(
            FullName: "Duplicate Nip Employee",
            Nip: "EMP-001", // This is seeded in EntityBuilder
            Gender: Gender.Female,
            PositionId: EntityBuilder.HrManagerId,
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: null
        );

        var response = await client.PostAsJsonAsync("/employees", req);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateEmployee_Unauthenticated_Returns401()
    {
        var req = new CreateEmployeeRequest(
            FullName: "Integration Test Employee",
            Nip: "EMP-ANON",
            Gender: Gender.Male,
            PositionId: EntityBuilder.SeniorDevId,
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: null
        );

        var response = await _factory.CreateAnonClient()
            .PostAsJsonAsync("/employees", req);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateEmployee_ViewerRole_Returns403()
    {
        var req = new CreateEmployeeRequest(
            FullName: "Integration Test Employee",
            Nip: "EMP-VIEW",
            Gender: Gender.Male,
            PositionId: EntityBuilder.SeniorDevId,
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: null
        );

        var response = await _factory.CreateViewerClient()
            .PostAsJsonAsync("/employees", req);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── GET /employees ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllEmployees_Returns200WithList()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/employees");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<IEnumerable<EmployeeDto>>>();
        body!.Data.Should().HaveCountGreaterThanOrEqualTo(2);
        body.Meta.Should().NotBeNull();
    }

    // ── GET /employees/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetEmployeeById_ValidId_Returns200()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/employees/{EntityBuilder.Employee1Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<EmployeeDto>>();
        body!.Data!.Nip.Should().Be("EMP-001");
    }

    [Fact]
    public async Task GetEmployeeById_NotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/employees/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /employees/{id}/position-histories ──────────────────────────────
    
    [Fact]
    public async Task GetPositionHistories_ValidId_Returns200()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/employees/{EntityBuilder.Employee1Id}/position-histories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<IEnumerable<PositionHistoryDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    // ── PATCH /employees/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateEmployee_ValidData_Returns200()
    {
        var client = _factory.CreateAdminClient();

        var req = new UpdateEmployeeRequest(
            FullName: "Updated Employee Integration",
            Nip: null,
            Gender: null,
            PositionId: null,
            DateOfJoining: null,
            DateOfActivePosition: null,
            EmployeeStatus: null,
            IsActive: null,
            UserId: null,
            DepartmentId: null,
            ManagerId: null
        );

        var response = await client.PatchAsJsonAsync($"/employees/{EntityBuilder.Employee1Id}", req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<EmployeeDto>>();
        body!.Data!.FullName.Should().Be("Updated Employee Integration");
    }

    // ── DELETE /employees/{id} ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteEmployee_ExistingEmployee_Returns200()
    {
        var client = _factory.CreateAdminClient();

        // Create an employee to delete
        var req = new CreateEmployeeRequest(
            FullName: "To Delete Employee",
            Nip: $"DEL-{Guid.NewGuid():N}",
            Gender: Gender.Male,
            PositionId: EntityBuilder.SeniorDevId,
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: null
        );

        var createResp = await client.PostAsJsonAsync("/employees", req);
        var created = await createResp.Content.ReadFromJsonAsync<Response<EmployeeDto>>();

        var response = await client.DeleteAsync($"/employees/{created!.Data!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
