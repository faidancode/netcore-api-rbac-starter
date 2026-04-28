using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Common.Extensions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Common.Models;
using netcore_api_rbac_starter.Modules.Auth.Dtos;
using netcore_api_rbac_starter.Modules.Roles.Dtos;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter.Modules.Roles;

public interface IRolesService
{
    Task<RoleDto> CreateAsync(CreateRoleRequest request);
    Task<PagedResult<RoleDto>> GetAllAsync(ListRoleQuery query);
    Task<RoleDto> GetByIdAsync(Guid id);
    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request);
    Task DeleteAsync(Guid id);
    Task<RoleDto> AssignPermissionsAsync(Guid id, AssignPermissionsRequest request);
}

public class RolesService : IRolesService
{
    private readonly AppDbContext _db;

    public RolesService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request)
    {
        var exists = await _db.Roles.AnyAsync(r => r.Name == request.Name);
        if (exists)
            throw new ConflictException($"Role '{request.Name}' already exists.");

        var role = new Role { Name = request.Name, Description = request.Description };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(role.Id);
    }

    public async Task<PagedResult<RoleDto>> GetAllAsync(ListRoleQuery query)
    {
        var term = (query.Search ?? query.Q)?.Trim();
        var dbQuery = _db.Roles.AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{term}%";
            dbQuery = dbQuery.Where(d => EF.Functions.ILike(d.Name, pattern) ||
                                         (d.Description != null && EF.Functions.ILike(d.Description, pattern)));
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

        return new PagedResult<RoleDto>
        {
            Items = items,
            Total = total
        };
    }

    public async Task<PagedResult<RoleDto>> GetPagedAsync(int page, int limit)
    {
        page = page < 1 ? 1 : page;
        limit = limit < 1 ? 10 : Math.Min(limit, 100);

        var query = _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name);

        var total = await query.CountAsync();
        var items = await query
            .ApplyPagination(page, limit)
            .ToListAsync();

        return new PagedResult<RoleDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Total = total
        };
    }

    public async Task<RoleDto> GetByIdAsync(Guid id)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Role", id);

        return MapToDto(role);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Role", id);

        if (request.Name != null && request.Name != role.Name)
        {
            var nameExists = await _db.Roles.AnyAsync(r => r.Name == request.Name);
            if (nameExists)
                throw new ConflictException($"Role '{request.Name}' already exists.");
            role.Name = request.Name;
        }

        if (request.Description != null) role.Description = request.Description;

        await _db.SaveChangesAsync();
        return await GetByIdAsync(role.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Role", id);

        role.IsDeleted = true;
        role.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<RoleDto> AssignPermissionsAsync(Guid id, AssignPermissionsRequest request)
    {
        // 1. Fetch Role (Throws 404 if not found)
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Role", id);

        var permissionIds = request.PermissionIds.Distinct().ToList();

        // 2. Validate Permissions (Ensures 404 if any ID is "Unknown")
        if (permissionIds.Any())
        {
            var existingCount = await _db.Permissions
                .CountAsync(p => permissionIds.Contains(p.Id));

            if (existingCount != permissionIds.Count)
            {
                // We use a generic ID label to satisfy the exception constructor
                throw new NotFoundException("Permission", "provided list");
            }
        }

        // 3. Clear and Update
        _db.RolePermissions.RemoveRange(role.RolePermissions);

        if (permissionIds.Any())
        {
            var newPermissions = permissionIds.Select(permId => new RolePermission
            {
                RoleId = id,
                PermissionId = permId
            });

            await _db.RolePermissions.AddRangeAsync(newPermissions);
        }

        // 4. Atomic Save
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    private static RoleDto MapToDto(Role r) => new(
        r.Id, r.Name, r.Description,
        r.RolePermissions.Select(rp => new PermissionDto(
            rp.Permission.Id, rp.Permission.Action, rp.Permission.Subject,
            rp.Permission.Conditions, rp.Permission.Fields)),
        r.CreatedAt, r.UpdatedAt
    );
}
