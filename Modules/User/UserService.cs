using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Common.Extensions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Users.Dtos;
using netcore_api_rbac_starter.Security;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter.Modules.Users;

public interface IUsersService
{
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<PagedResult<UserDto>> GetAllAsync(ListUsersQuery query, CancellationToken ct);
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct);
    Task ChangePasswordAsync(Guid id, ChangeUserPasswordRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
}

public class UsersService : IUsersService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService? _currentUser;

    public UsersService(AppDbContext db, ICurrentUserService? currentUser = null)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        // 🔥 normalize email (prevent logical duplicate)
        var email = request.Email.Trim().ToLowerInvariant();

        // 🔥 parallel validation → reduce DB round trip
        var emailExistsTask = _db.Users.AnyAsync(u => u.Email == email, ct);

        var roleExistsTask = request.RoleId.HasValue
            ? _db.Roles.AnyAsync(r => r.Id == request.RoleId.Value, ct)
            : Task.FromResult(true);

        await Task.WhenAll(emailExistsTask, roleExistsTask);

        if (emailExistsTask.Result)
            throw new ConflictException($"User with email '{request.Email}' already exists.");

        if (!roleExistsTask.Result)
            throw new NotFoundException("Role", request.RoleId!.Value);

        var user = new User
        {
            Name = request.Name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // 🔐 secure hashing
            RoleId = request.RoleId,
            IsActive = request.IsActive
        };

        _db.Users.Add(user);

        // ✅ single write → no transaction needed (EF already atomic)
        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(user.Id, ct);
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(ListUsersQuery query, CancellationToken ct)
    {
        var term = (query.Search ?? query.Q)?.Trim();

        var dbQuery = _db.Users
            .Include(u => u.Role) // ✅ needed for RoleName in DTO
            .AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{term}%";
            dbQuery = dbQuery.Where(u =>
                EF.Functions.ILike(u.Name, pattern));
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var limit = query.Limit < 1 ? 10 : Math.Min(query.Limit, 100);

        var sortParam = query.Sort ?? "createdAt:desc";
        dbQuery = dbQuery.ApplySorting(sortParam);

        var total = await dbQuery.CountAsync(ct);

        var items = await dbQuery
            .ApplyPagination(page, limit)
            .Select(u => MapToDto(u))
            .ToListAsync(ct);

        return new PagedResult<UserDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Limit = limit
        };
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User", id);

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User", id);

        // 🔥 email update with normalization + correct uniqueness check
        if (request.Email != null)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (email != user.Email)
            {
                var emailExists = await _db.Users
                    .AnyAsync(u => u.Email == email && u.Id != id, ct);

                if (emailExists)
                    throw new ConflictException($"Email '{request.Email}' is already in use.");

                user.Email = email;
            }
        }

        if (request.Name != null)
            user.Name = request.Name;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (request.RoleId != null)
        {
            if (request.RoleId == Guid.Empty)
            {
                user.RoleId = null;
            }
            else
            {
                var roleExists = await _db.Roles
                    .AnyAsync(r => r.Id == request.RoleId.Value, ct);

                if (!roleExists)
                    throw new NotFoundException("Role", request.RoleId.Value);

                user.RoleId = request.RoleId;
            }
        }

        await _db.SaveChangesAsync(ct);

        return await GetByIdAsync(user.Id, ct);
    }

    public async Task ChangePasswordAsync(Guid id, ChangeUserPasswordRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User", id);

        var isSelfService = _currentUser?.IsAuthenticated == true &&
                            _currentUser.UserId == id;

        // 🔐 self-service must verify current password
        if (isSelfService)
        {
            var currentPassword = request.CurrentPassword?.Trim();

            if (string.IsNullOrWhiteSpace(currentPassword) ||
                !BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                throw new UnauthorizedException("Current password is invalid.");
            }
        }

        // 💡 assume admin flow handled by permission layer
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("User", id);

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static UserDto MapToDto(User u) => new(
        u.Id,
        u.Name,
        u.Email,
        u.IsActive,
        u.RoleId,
        u.Role?.Name,
        u.CreatedAt,
        u.UpdatedAt
    );
}
