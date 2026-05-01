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
    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken ct);
    Task<PagedResult<RoleDto>> GetAllAsync(ListRoleQuery query, CancellationToken ct);
    Task<IEnumerable<PermissionDto>> GetPermissionsAsync(CancellationToken ct);
    Task<RoleDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct);
    Task<RoleDto> AssignPermissionsAsync(Guid id, AssignPermissionsRequest request, CancellationToken ct);
}

public class RolesService : IRolesService
{
    private readonly AppDbContext _db;

    public RolesService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 🔍 validation
        var exists = await _db.Roles.AnyAsync(r => r.Name == request.Name, ct);
        if (exists)
            throw new ConflictException($"Role '{request.Name}' already exists.");

        var permissionIds = await NormalizeAndValidatePermissionIdsAsync(request.PermissionIds, ct);

        var role = new Role
        {
            Name = request.Name,
            Description = request.Description
        };

        _db.Roles.Add(role);

        // 🔥 no SaveChanges here → wait until all operations done
        await ReplacePermissionsAsync(role.Id, permissionIds, ct);

        await _db.SaveChangesAsync(ct); // ✅ single commit (prevent partial write)
        await tx.CommitAsync(ct);

        return await GetByIdAsync(role.Id, ct);
    }

    public async Task<PagedResult<RoleDto>> GetAllAsync(ListRoleQuery query, CancellationToken ct)
    {
        var term = (query.Search ?? query.Q)?.Trim();
        var dbQuery = _db.Roles.AsQueryable();

        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{term}%";
            dbQuery = dbQuery.Where(d =>
                EF.Functions.ILike(d.Name, pattern) ||
                (d.Description != null && EF.Functions.ILike(d.Description, pattern)));
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var limit = query.Limit < 1 ? 10 : Math.Min(query.Limit, 100);

        var sortParam = query.Sort ?? "createdAt:desc";
        dbQuery = dbQuery.ApplySorting(sortParam);

        var total = await dbQuery.CountAsync(ct);

        var items = await dbQuery
            .ApplyPagination(page, limit)
            .Select(d => MapToDto(d))
            .ToListAsync(ct);

        return new PagedResult<RoleDto>
        {
            Items = items,
            Total = total,
            Page = page,
            Limit = limit
        };
    }

    public async Task<IEnumerable<PermissionDto>> GetPermissionsAsync(CancellationToken ct)
    {
        return await _db.Permissions
            .OrderBy(p => p.Subject)
            .ThenBy(p => p.Action)
            .Select(p => new PermissionDto(
                p.Id, p.Action, p.Subject, p.Conditions, p.Fields))
            .ToListAsync(ct);
    }

    public async Task<RoleDto> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Role", id);

        return MapToDto(role);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Role", id);

        if (request.Name != null && request.Name != role.Name)
        {
            var nameExists = await _db.Roles.AnyAsync(r => r.Name == request.Name, ct);
            if (nameExists)
                throw new ConflictException($"Role '{request.Name}' already exists.");

            role.Name = request.Name;
        }

        if (request.Description != null)
            role.Description = request.Description;

        var permissionIds = await NormalizeAndValidatePermissionIdsAsync(request.PermissionIds, ct);

        await ReplacePermissionsAsync(role.Id, permissionIds, ct);

        await _db.SaveChangesAsync(ct); // ✅ single commit
        await tx.CommitAsync(ct);

        return await GetByIdAsync(role.Id, ct);
    }

    public async Task<RoleDto> AssignPermissionsAsync(Guid id, AssignPermissionsRequest request, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Role", id);

        var permissionIds = await NormalizeAndValidatePermissionIdsAsync(request.PermissionIds, ct);

        await ReplacePermissionsAsync(role.Id, permissionIds, ct);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("Role", id);

        role.IsDeleted = true;
        role.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct); // ✅ single write → no transaction needed
    }

    // 🔥 Validate permission IDs (no unnecessary data load)
    private async Task<List<Guid>> NormalizeAndValidatePermissionIdsAsync(
        IEnumerable<Guid> permissionIds,
        CancellationToken ct)
    {
        var distinctIds = permissionIds.Distinct().ToList();

        if (distinctIds.Count == 0)
            return distinctIds;

        var existingIds = await _db.Permissions
            .Where(p => distinctIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (existingIds.Count != distinctIds.Count)
            throw new NotFoundException("Permission", "provided list");

        return distinctIds;
    }

    // 🔥 Replace role-permissions efficiently
    private async Task ReplacePermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken ct)
    {
        // ✅ direct delete (EF Core 7+)
        await _db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ExecuteDeleteAsync(ct);

        if (permissionIds.Count == 0)
            return;

        var newPermissions = permissionIds.Select(permId => new RolePermission
        {
            RoleId = roleId,
            PermissionId = permId
        });

        await _db.RolePermissions.AddRangeAsync(newPermissions, ct);
    }

    private static RoleDto MapToDto(Role r) => new(
        r.Id,
        r.Name,
        r.Description,
        r.RolePermissions.Select(rp => new PermissionDto(
            rp.Permission.Id,
            rp.Permission.Action,
            rp.Permission.Subject,
            rp.Permission.Conditions,
            rp.Permission.Fields)),
        r.CreatedAt,
        r.UpdatedAt
    );
}
