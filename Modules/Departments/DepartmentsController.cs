using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Departments.Dtos;
using netcore_api_rbac_starter.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace netcore_api_rbac_starter.Modules.Departments;

[ApiController]
[Route("departments")]
[Authorize]
[Produces("application/json")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentsService _service;

    public DepartmentsController(IDepartmentsService service) => _service = service;

    [HttpPost]
    [HasPermission("create", "Department")]
    public async Task<ActionResult<Response<DepartmentDto>>> Create([FromBody] CreateDepartmentRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            Response<DepartmentDto>.Ok(result, "Department created successfully."));
    }

    [HttpGet]
    [HasPermission("read", "Department")]
    public async Task<ActionResult<Response<IEnumerable<DepartmentDto>>>> GetAll([FromQuery] ListDepartmentQuery query)
    {
        var result = await _service.GetAllAsync(query);
        return Ok(Response<IEnumerable<DepartmentDto>>.Ok(
            result.Items, 
            meta: PaginationMeta.Create(query.Page, query.Limit, result.Total)
        ));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("read", "Department")]
    public async Task<ActionResult<Response<DepartmentDto>>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(Response<DepartmentDto>.Ok(result));
    }

    [HttpPatch("{id:guid}")]
    [HasPermission("update", "Department")]
    public async Task<ActionResult<Response<DepartmentDto>>> Update(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(Response<DepartmentDto>.Ok(result, "Department updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("delete", "Department")]
    public async Task<ActionResult<Response<object?>>> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return Ok(Response<object?>.Ok(null, "Department deleted successfully."));
    }
}