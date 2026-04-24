using netcore_api_rbac_starter.Common.Exceptions;
using netcore_api_rbac_starter.Entities;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Modules.Auth.Dtos;
using netcore_api_rbac_starter.Modules.Roles.Dtos;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter.Modules.Roles;

public interface IRolesService
{
    Task<RoleDto> CreateAsync(CreateRoleRequest request);
    Task<IEnumerable<RoleDto>> GetAllAsync();
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

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _db.Roles
            .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync();

        return roles.Select(MapToDto);
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
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException("Role", id);

        // Validate all permission IDs exist
        var permissionIds = request.PermissionIds.ToList();
        if (permissionIds.Any())
        {
            var existingCount = await _db.Permissions
                .CountAsync(p => permissionIds.Contains(p.Id));

            if (existingCount != permissionIds.Count)
                throw new NotFoundException("One or more permissions not found.");
        }

        // Clear existing and replace (transactional)
        await using var transaction = await _db.Database.BeginTransactionAsync();

        _db.RolePermissions.RemoveRange(role.RolePermissions);
        await _db.SaveChangesAsync();

        foreach (var permId in permissionIds)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = id,
                PermissionId = permId
            });
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

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