using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Common.Extensions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Users.Dtos;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter.Modules.Users;

public interface IUsersService
{
    Task<UserDto> CreateAsync(CreateUserRequest request);
    Task<PagedResult<UserDto>> GetAllAsync(ListUsersQuery query);

    Task<UserDto> GetByIdAsync(Guid id);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request);
    Task DeleteAsync(Guid id);
}

public class UsersService : IUsersService
{
    private readonly AppDbContext _db;

    public UsersService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (exists)
            throw new ConflictException($"User with email '{request.Email}' already exists.");

        if (request.RoleId.HasValue)
        {
            var roleExists = await _db.Roles.AnyAsync(r => r.Id == request.RoleId.Value);
            if (!roleExists)
                throw new NotFoundException("Role", request.RoleId.Value);
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = request.RoleId,
            IsActive = request.IsActive
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(user.Id);
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(ListUsersQuery query)
    {
        var term = (query.Search ?? query.Q)?.Trim();
        var dbQuery = _db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{term}%";
            dbQuery = dbQuery.Where(d => EF.Functions.ILike(d.Name, pattern));
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var limit = query.Limit < 1 ? 10 : Math.Min(query.Limit, 100);

        var sortParam = query.Sort ?? "createdAt:desc";
        dbQuery = dbQuery.ApplySorting(sortParam);

        var total = await dbQuery.CountAsync();
        var items = await dbQuery
            .ApplyPagination(page, limit)
            .Select(d => MapToDto(d))
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Limit = limit
        };
    }

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException("User", id);

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException("User", id);

        if (request.Email != null && request.Email != user.Email)
        {
            var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
                throw new ConflictException($"Email '{request.Email}' is already in use.");
            user.Email = request.Email;
        }

        if (request.Name != null) user.Name = request.Name;
        if (request.Password != null) user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        if (request.RoleId != null)
        {
            if (request.RoleId == Guid.Empty)
            {
                user.RoleId = null;
            }
            else
            {
                var roleExists = await _db.Roles.AnyAsync(r => r.Id == request.RoleId.Value);
                if (!roleExists)
                    throw new NotFoundException("Role", request.RoleId.Value);
                user.RoleId = request.RoleId;
            }
        }

        await _db.SaveChangesAsync();
        return await GetByIdAsync(user.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException("User", id);

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static UserDto MapToDto(User u) => new(
        u.Id, u.Name, u.Email, u.IsActive,
        u.RoleId, u.Role?.Name,
        u.CreatedAt, u.UpdatedAt
    );
}
