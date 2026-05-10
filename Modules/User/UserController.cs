using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Users.Dtos;
using netcore_api_rbac_starter.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace netcore_api_rbac_starter.Modules.Users;

[ApiController]
[Route("api/v{version:apiVersion}/users")]
[Authorize]
[EnableRateLimiting("per-user")]
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
    public async Task<ActionResult<Response<UserDto>>> Create([FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        var result = await _usersService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            Response<UserDto>.Ok(result, "User created successfully."));
    }

    [HttpGet]
    [HasPermission("read", "User")]
    public async Task<ActionResult<Response<IEnumerable<UserDto>>>> GetAll(
        [FromQuery] ListUsersQuery query,
        CancellationToken ct
        )
    {
        var result = await _usersService.GetAllAsync(query, ct);
        return Ok(Response<IEnumerable<UserDto>>.Ok(
            result.Items,
            meta: PaginationMeta.Create(result.Page, result.Limit, result.Total)
        ));
    }

    [HttpGet("{id:guid}")]
    [HasPermission("read", "User")]
    public async Task<ActionResult<Response<UserDto>>> GetById(Guid id,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");
        var result = await _usersService.GetByIdAsync(id, ct);
        return Ok(Response<UserDto>.Ok(result));
    }

    [HttpPatch("{id:guid}")]
    [HasPermission("update", "User")]
    public async Task<ActionResult<Response<UserDto>>> Update(Guid id, [FromBody] UpdateUserRequest request,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");
        var result = await _usersService.UpdateAsync(id, request, ct);
        return Ok(Response<UserDto>.Ok(result, "User updated successfully."));
    }

    [HttpPatch("{id:guid}/password")]
    [HasPermission("update", "User")]
    public async Task<ActionResult<Response<object?>>> ChangePassword(
        Guid id,
        [FromBody] ChangeUserPasswordRequest request,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");
        await _usersService.ChangePasswordAsync(id, request, ct);
        return Ok(Response<object?>.Ok(null, "Password updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission("delete", "User")]
    public async Task<ActionResult<Response<object?>>> Delete(Guid id,
        CancellationToken ct)
    {
        if (id == Guid.Empty)
            throw new BadHttpRequestException("Invalid ID");
        await _usersService.DeleteAsync(id, ct);
        return Ok(Response<object?>.Ok(null, "User deleted successfully."));
    }
}
