using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Dashboard.Dtos;
using netcore_api_rbac_starter.Tests.Helpers;

namespace netcore_api_rbac_starter.Tests.Integration.Dashboard;

public class DashboardIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public DashboardIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetDashboard_ReturnsSummary()
    {
        var client = _factory.CreateAdminClient();

        var response = await client.GetAsync("/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<Response<DashboardSummaryDto>>();
        body!.Success.Should().BeTrue();
        body.Data!.TotalDepartments.Should().Be(2);
        body.Data!.TotalPositions.Should().Be(2);
        body.Data!.TotalEmployees.Should().Be(2);
        body.Data!.TotalMaleEmployees.Should().Be(1);
        body.Data!.TotalFemaleEmployees.Should().Be(1);
    }

    [Fact]
    public async Task GetDashboard_Unauthenticated_Returns401()
    {
        var response = await _factory.CreateAnonClient().GetAsync("/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
