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
    Task<IEnumerable<PermissionDto>> GetPermissionsAsync();
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
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var exists = await _db.Roles.AnyAsync(r => r.Name == request.Name);
            if (exists)
                throw new ConflictException($"Role '{request.Name}' already exists.");

            var permissionIds = await NormalizeAndValidatePermissionIdsAsync(request.PermissionIds);

            var role = new Role { Name = request.Name, Description = request.Description };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            await ReplacePermissionsAsync(role.Id, permissionIds);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return await GetByIdAsync(role.Id);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
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
            Total = total,
            Page = page,
            Limit = limit
        };
    }

    public async Task<IEnumerable<PermissionDto>> GetPermissionsAsync()
    {
        return await _db.Permissions
            .OrderBy(p => p.Subject)
            .ThenBy(p => p.Action)
            .Select(p => new PermissionDto(
                p.Id, p.Action, p.Subject, p.Conditions, p.Fields))
            .ToListAsync();
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
            Total = total,
            Page = page,
            Limit = limit
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
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var role = await _db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new NotFoundException("Role", id);

            if (request.Name != null && request.Name != role.Name)
            {
                var nameExists = await _db.Roles.AnyAsync(r => r.Name == request.Name);
                if (nameExists)
                    throw new ConflictException($"Role '{request.Name}' already exists.");
                role.Name = request.Name;
            }

            if (request.Description != null) role.Description = request.Description;

            var permissionIds = await NormalizeAndValidatePermissionIdsAsync(request.PermissionIds);
            await ReplacePermissionsAsync(role.Id, permissionIds);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return await GetByIdAsync(role.Id);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
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
        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var role = await _db.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new NotFoundException("Role", id);

            var permissionIds = await NormalizeAndValidatePermissionIdsAsync(request.PermissionIds);
            await ReplacePermissionsAsync(role.Id, permissionIds);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return await GetByIdAsync(id);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<List<Guid>> NormalizeAndValidatePermissionIdsAsync(IEnumerable<Guid> permissionIds)
    {
        var distinctIds = permissionIds.Distinct().ToList();

        if (distinctIds.Count == 0)
        {
            return distinctIds;
        }

        var existingCount = await _db.Permissions
            .CountAsync(p => distinctIds.Contains(p.Id));

        if (existingCount != distinctIds.Count)
        {
            throw new NotFoundException("Permission", "provided list");
        }

        return distinctIds;
    }

    private async Task ReplacePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds)
    {
        var existingRolePermissions = await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();

        _db.RolePermissions.RemoveRange(existingRolePermissions);

        if (permissionIds.Count == 0)
        {
            return;
        }

        var newPermissions = permissionIds.Select(permId => new RolePermission
        {
            RoleId = roleId,
            PermissionId = permId
        });

        await _db.RolePermissions.AddRangeAsync(newPermissions);
    }

    private static RoleDto MapToDto(Role r) => new(
        r.Id, r.Name, r.Description,
        r.RolePermissions.Select(rp => new PermissionDto(
            rp.Permission.Id, rp.Permission.Action, rp.Permission.Subject,
            rp.Permission.Conditions, rp.Permission.Fields)),
        r.CreatedAt, r.UpdatedAt
    );
}
