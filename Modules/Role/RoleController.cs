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
    public async Task<ActionResult<ApiResponse<RoleDto>>> Create([FromBody] CreateRoleRequest request)
    {
        var result = await _rolesService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<RoleDto>.Ok(result, "Role created successfully."));
    }

    [HttpGet]
    [HasPermission("read", "Role")]
    public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetAll()
    {
        var result = await _rolesService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("read", "Role")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> GetById(Guid id)
    {
        var result = await _rolesService.GetByIdAsync(id);
        return Ok(ApiResponse<RoleDto>.Ok(result));
    }

    [HttpPatch("{id:guid}")]
    [HasPermission("update", "Role")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> Update(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var result = await _rolesService.UpdateAsync(id, request);
        return Ok(ApiResponse<RoleDto>.Ok(result, "Role updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("delete", "Role")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _rolesService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("Role deleted successfully."));
    }

    [HttpPost("{id:guid}/permissions")]
    [HasPermission("update", "Role")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> AssignPermissions(
        Guid id,
        [FromBody] AssignPermissionsRequest request)
    {
        var result = await _rolesService.AssignPermissionsAsync(id, request);
        return Ok(ApiResponse<RoleDto>.Ok(result, "Permissions assigned successfully."));
    }
}