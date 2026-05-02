using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Employees.Dtos;
using netcore_api_rbac_starter.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace netcore_api_rbac_starter.Modules.Employees;

[ApiController]
[Route("api/v{version:apiVersion}/employees")]
[Authorize]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeesService _service;

    public EmployeesController(IEmployeesService service) => _service = service;

    [HttpPost]
    [HasPermission("create", "Employee")]
    public async Task<ActionResult<Response<EmployeeDto>>> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken ct) // ✅ inject dari HttpContext
    {
        var result = await _service.CreateAsync(request, ct);

        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            Response<EmployeeDto>.Ok(result, "Employee created successfully."));
    }

    [HttpGet]
    [HasPermission("read", "Employee")]
    public async Task<ActionResult<Response<IEnumerable<EmployeeDto>>>> GetAll(
        [FromQuery] EmployeeListQuery query,
        CancellationToken ct)
    {
        var result = await _service.GetAllAsync(query, ct);

        return Ok(Response<IEnumerable<EmployeeDto>>.Ok(
            result.Items,
            meta: PaginationMeta.Create(result.Page, result.Limit, result.Total)
        ));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("read", "Employee")]
    public async Task<ActionResult<Response<EmployeeDto>>> GetById(
        Guid id,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");

        var result = await _service.GetByIdAsync(id, ct);

        return Ok(Response<EmployeeDto>.Ok(result));
    }

    [HttpGet("{id:guid}/position-histories")]
    [HasPermission("read", "Employee")]
    public async Task<ActionResult<Response<IEnumerable<PositionHistoryDto>>>> GetPositionHistories(
        Guid id,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");

        var result = await _service.GetPositionHistoriesAsync(id, ct);

        return Ok(Response<IEnumerable<PositionHistoryDto>>.Ok(result));
    }

    [HttpPatch("{id:guid}")]
    [HasPermission("update", "Employee")]
    public async Task<ActionResult<Response<EmployeeDto>>> Update(
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");

        var result = await _service.UpdateAsync(id, request, ct);

        return Ok(Response<EmployeeDto>.Ok(result, "Employee updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("delete", "Employee")]
    public async Task<ActionResult<Response<object?>>> Delete(
        Guid id,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");

        await _service.DeleteAsync(id, ct);

        return Ok(Response<object?>.Ok(null, "Employee deleted successfully."));
    }
}