using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Modules.Employees.Dtos;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Integration.Employees;

public class EmployeesIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EmployeesIntegrationTests(ApiFactory factory) => _factory = factory;

    // ── POST /api/v1/employees ─────────────────────────────────────────────────────

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

        var response = await client.PostAsJsonAsync("/api/v1/employees", req);

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

        var response = await client.PostAsJsonAsync("/api/v1/employees", req);
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
            .PostAsJsonAsync("/api/v1/employees", req);
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
            .PostAsJsonAsync("/api/v1/employees", req);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateEmployee_DuplicateRequestWithIdempotencyKey_ReturnsSameResponseAndDoesNotDuplicate()
    {
        // Arrange
        var client = _factory.CreateAdminClient();
        var idempotencyKey = Guid.NewGuid().ToString();
        var nip = $"EMP-{Guid.NewGuid():N}";

        var req = new CreateEmployeeRequest(
            FullName: "Idempotent Employee",
            Nip: nip,
            Gender: Gender.Male,
            PositionId: EntityBuilder.SeniorDevId,
            DateOfJoining: new DateOnly(2023, 1, 1),
            DateOfActivePosition: new DateOnly(2023, 1, 1),
            DepartmentId: EntityBuilder.EngineeringId
        );

        // Tambahkan header Idempotency-Key
        client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);

        // Act 1: Request Pertama
        var response1 = await client.PostAsJsonAsync("/api/v1/employees", req);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        var body1 = await response1.Content.ReadAsStringAsync();

        // Act 2: Request Kedua dengan Key yang sama
        var response2 = await client.PostAsJsonAsync("/api/v1/employees", req);
        var body2 = await response2.Content.ReadAsStringAsync();

        // Assert
        // 1. Response status harus tetap sukses (sesuai logic middleware Anda)
        response2.StatusCode.Should().Be(response1.StatusCode);
        // 2. Body response harus identik dengan yang pertama
        body2.Should().Be(body1);

        // 3. Verifikasi Database: Pastikan tidak ada duplikasi data
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employeeCount = await db.Employees.CountAsync(e => e.Nip == nip);

        employeeCount.Should().Be(1); // Tetap 1, bukan 2
    }

    [Fact]
    public async Task CreateEmployee_SameKeyDifferentUser_ShouldCreateNewRecord()
    {
        // Arrange
        var adminClient = _factory.CreateAdminClient(); // User A
        var managerClient = _factory.CreateManagerClient(); // User B
        var idempotencyKey = "shared-key-123";

        var req1 = new CreateEmployeeRequest("User A Employee", "NIP-A", Gender.Male, EntityBuilder.SeniorDevId, new DateOnly(2023, 1, 1), null, DepartmentId: EntityBuilder.EngineeringId);
        var req2 = new CreateEmployeeRequest("User B Employee", "NIP-B", Gender.Female, EntityBuilder.SeniorDevId, new DateOnly(2023, 1, 1), null, DepartmentId: EntityBuilder.EngineeringId);

        // Act - Request 1 (Admin)
        using var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/employees")
        {
            Content = JsonContent.Create(req1)
        };
        request1.Headers.Add("Idempotency-Key", idempotencyKey);
        var res1 = await adminClient.SendAsync(request1);

        // Act - Request 2 (Manager)
        using var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/employees")
        {
            Content = JsonContent.Create(req2)
        };
        request2.Headers.Add("Idempotency-Key", idempotencyKey);
        var res2 = await managerClient.SendAsync(request2);

        // Assert
        res1.StatusCode.Should().Be(HttpStatusCode.Created);
        res2.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ── GET /api/v1/employees ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllEmployees_Returns200WithList()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync("/api/v1/employees");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<IEnumerable<EmployeeDto>>>();
        body!.Data.Should().HaveCountGreaterThanOrEqualTo(2);
        body.Meta.Should().NotBeNull();
    }

    // ── GET /api/v1/employees/{id} ─────────────────────────────────────────────────

    [Fact]
    public async Task GetEmployeeById_ValidId_Returns200()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/api/v1/employees/{EntityBuilder.Employee1Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<EmployeeDto>>();
        body!.Data!.Nip.Should().Be("EMP-001");
    }

    [Fact]
    public async Task GetEmployeeById_NotFound_Returns404()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/api/v1/employees/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/v1/employees/{id}/position-histories ──────────────────────────────

    [Fact]
    public async Task GetPositionHistories_ValidId_Returns200()
    {
        var client = _factory.CreateAdminClient();
        var response = await client.GetAsync($"/api/v1/employees/{EntityBuilder.Employee1Id}/position-histories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<IEnumerable<PositionHistoryDto>>>();
        body!.Data.Should().NotBeEmpty();
    }

    // ── PATCH /api/v1/employees/{id} ───────────────────────────────────────────────

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

        var response = await client.PatchAsJsonAsync($"/api/v1/employees/{EntityBuilder.Employee1Id}", req);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Response<EmployeeDto>>();
        body!.Data!.FullName.Should().Be("Updated Employee Integration");
    }

    // ── DELETE /api/v1/employees/{id} ──────────────────────────────────────────────

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

        var createResp = await client.PostAsJsonAsync("/api/v1/employees", req);
        var created = await createResp.Content.ReadFromJsonAsync<Response<EmployeeDto>>();

        var response = await client.DeleteAsync($"/api/v1/employees/{created!.Data!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
