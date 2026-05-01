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
    public async Task<ActionResult<Response<PositionDto>>> Create([FromBody] CreatePositionRequest request,
        CancellationToken ct)
    {
        var result = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            Response<PositionDto>.Ok(result, "Position created successfully."));
    }

    [HttpGet]
    [HasPermission("read", "Position")]
    public async Task<ActionResult<Response<IEnumerable<PositionDto>>>> GetAll(
        [FromQuery] ListPositionQuery query,
        CancellationToken ct
        )
    {
        var result = await _service.GetAllAsync(query, ct);
        return Ok(Response<IEnumerable<PositionDto>>.Ok(
            result.Items,
            meta: PaginationMeta.Create(result.Page, result.Limit, result.Total)
        ));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("read", "Position")]
    public async Task<ActionResult<Response<PositionDto>>> GetById(Guid id,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(Response<PositionDto>.Ok(result));
    }

    [HttpPatch("{id:guid}")]
    [HasPermission("update", "Position")]
    public async Task<ActionResult<Response<PositionDto>>> Update(Guid id, [FromBody] UpdatePositionRequest request,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");
        var result = await _service.UpdateAsync(id, request, ct);
        return Ok(Response<PositionDto>.Ok(result, "Position updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("delete", "Position")]
    public async Task<ActionResult<Response<object?>>> Delete(Guid id,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");
        await _service.DeleteAsync(id, ct);
        return Ok(Response<object?>.Ok(null, "Position deleted successfully."));
    }
}