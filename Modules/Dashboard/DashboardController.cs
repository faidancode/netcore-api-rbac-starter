using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Dashboard.Dtos;

namespace netcore_api_rbac_starter.Modules.Dashboard;

[ApiController]
[Route("api/v{version:apiVersion}/dashboard")]
[Authorize]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<Response<DashboardSummaryDto>>> GetSummary()
    {
        var result = await _service.GetSummaryAsync();
        return Ok(Response<DashboardSummaryDto>.Ok(result));
    }
}
