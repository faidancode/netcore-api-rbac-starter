using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Roles.Dtos;
using netcore_api_rbac_starter.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace netcore_api_rbac_starter.Modules.Roles;

[ApiController]
[Route("roles")]
[Authorize]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly IRolesService _rolesService;

    public RolesController(IRolesService rolesService)
    {
        _rolesService = rolesService;
    }

    [HttpPost]
    [HasPermission("create", "Role")]
    public async Task<ActionResult<Response<RoleDto>>> Create([FromBody] CreateRoleRequest request)
    {
        var result = await _rolesService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            Response<RoleDto>.Ok(result, "Role created successfully."));
    }

    [HttpGet]
    [HasPermission("read", "Role")]
    public async Task<ActionResult<Response<IEnumerable<RoleDto>>>> GetAll(
        [FromQuery] ListRoleQuery query
        )
    {
        var result = await _rolesService.GetAllAsync(query);
        return Ok(Response<IEnumerable<RoleDto>>.Ok(
            result.Items,
            meta: PaginationMeta.Create(result.Page, result.Limit, result.Total)
        ));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("read", "Role")]
    public async Task<ActionResult<Response<RoleDto>>> GetById(Guid id)
    {
        var result = await _rolesService.GetByIdAsync(id);
        return Ok(Response<RoleDto>.Ok(result));
    }

    [HttpPatch("{id:guid}")]
    [HasPermission("update", "Role")]
    public async Task<ActionResult<Response<RoleDto>>> Update(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var result = await _rolesService.UpdateAsync(id, request);
        return Ok(Response<RoleDto>.Ok(result, "Role updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("delete", "Role")]
    public async Task<ActionResult<Response<object?>>> Delete(Guid id)
    {
        await _rolesService.DeleteAsync(id);
        return Ok(Response<object?>.Ok(null, "Role deleted successfully."));
    }

    [HttpPost("{id:guid}/permissions")]
    [HasPermission("update", "Role")]
    public async Task<ActionResult<Response<RoleDto>>> AssignPermissions(
        Guid id,
        [FromBody] AssignPermissionsRequest request)
    {
        var result = await _rolesService.AssignPermissionsAsync(id, request);
        return Ok(Response<RoleDto>.Ok(result, "Permissions assigned successfully."));
    }
}
