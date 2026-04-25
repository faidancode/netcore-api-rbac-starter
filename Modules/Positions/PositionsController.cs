using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Positions.Dtos;
using netcore_api_rbac_starter.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace netcore_api_rbac_starter.Modules.Positions;

[ApiController]
[Route("positions")]
[Authorize]
[Produces("application/json")]
public class PositionsController : ControllerBase
{
    private readonly IPositionsService _service;

    public PositionsController(IPositionsService service) => _service = service;

    [HttpPost]
    [HasPermission("create", "Position")]
    public async Task<ActionResult<Response<PositionDto>>> Create([FromBody] CreatePositionRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            Response<PositionDto>.Ok(result, "Position created successfully."));
    }

    [HttpGet]
    [HasPermission("read", "Position")]
    public async Task<ActionResult<Response<IEnumerable<PositionDto>>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(Response<IEnumerable<PositionDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("read", "Position")]
    public async Task<ActionResult<Response<PositionDto>>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(Response<PositionDto>.Ok(result));
    }

    [HttpPatch("{id:guid}")]
    [HasPermission("update", "Position")]
    public async Task<ActionResult<Response<PositionDto>>> Update(Guid id, [FromBody] UpdatePositionRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(Response<PositionDto>.Ok(result, "Position updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("delete", "Position")]
    public async Task<ActionResult<Response<object?>>> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return Ok(Response<object?>.Ok(null, "Position deleted successfully."));
    }
}