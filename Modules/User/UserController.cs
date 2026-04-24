using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Users.Dtos;
using netcore_api_rbac_starter.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace netcore_api_rbac_starter.Modules.Users;

[ApiController]
[Route("users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpPost]
    [HasPermission("create", "User")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        var result = await _usersService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<UserDto>.Ok(result, "User created successfully."));
    }

    [HttpGet]
    [HasPermission("read", "User")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAll()
    {
        var result = await _usersService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("read", "User")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
    {
        var result = await _usersService.GetByIdAsync(id);
        return Ok(ApiResponse<UserDto>.Ok(result));
    }

    [HttpPatch("{id:guid}")]
    [HasPermission("update", "User")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var result = await _usersService.UpdateAsync(id, request);
        return Ok(ApiResponse<UserDto>.Ok(result, "User updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("delete", "User")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id)
    {
        await _usersService.DeleteAsync(id);
        return Ok(ApiResponse.Ok("User deleted successfully."));
    }
}