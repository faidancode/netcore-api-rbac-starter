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
    public async Task<ActionResult<Response<UserDto>>> Create([FromBody] CreateUserRequest request)
    {
        var result = await _usersService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            Response<UserDto>.Ok(result, "User created successfully."));
    }

    [HttpGet]
    [HasPermission("read", "User")]
    public async Task<ActionResult<Response<IEnumerable<UserDto>>>> GetAll(
        [FromQuery] ListUsersQuery query
        )
    {
        var result = await _usersService.GetAllAsync(query);
        return Ok(Response<IEnumerable<UserDto>>.Ok(
            result.Items,
            meta: PaginationMeta.Create(result.Page, result.Limit, result.Total)
        ));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("read", "User")]
    public async Task<ActionResult<Response<UserDto>>> GetById(Guid id)
    {
        var result = await _usersService.GetByIdAsync(id);
        return Ok(Response<UserDto>.Ok(result));
    }

    [HttpPatch("{id:guid}")]
    [HasPermission("update", "User")]
    public async Task<ActionResult<Response<UserDto>>> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var result = await _usersService.UpdateAsync(id, request);
        return Ok(Response<UserDto>.Ok(result, "User updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("delete", "User")]
    public async Task<ActionResult<Response<object?>>> Delete(Guid id)
    {
        await _usersService.DeleteAsync(id);
        return Ok(Response<object?>.Ok(null, "User deleted successfully."));
    }
}
