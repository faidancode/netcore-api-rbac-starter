using netcore_api_rbac_starter.Entities;
using Microsoft.EntityFrameworkCore;

namespace netcore_api_rbac_starter.Data.Seed;

public static class DatabaseSeeder
{
    private static readonly Guid AdminRoleId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid ViewerRoleId = Guid.Parse("11111111-0000-0000-0000-000000000002");

    private static readonly Guid ManageAllPermissionId = Guid.Parse("66666666-0000-0000-0000-000000000001");
    private static readonly Guid ReadUserPermissionId = Guid.Parse("66666666-0000-0000-0000-000000000002");
    private static readonly Guid CreateUserPermissionId = Guid.Parse("66666666-0000-0000-0000-000000000003");
    private static readonly Guid UpdateUserPermissionId = Guid.Parse("66666666-0000-0000-0000-000000000004");
    private static readonly Guid DeleteUserPermissionId = Guid.Parse("66666666-0000-0000-0000-000000000005");
    private static readonly Guid ReadRolePermissionId = Guid.Parse("66666666-0000-0000-0000-000000000006");
    private static readonly Guid CreateRolePermissionId = Guid.Parse("66666666-0000-0000-0000-000000000007");
    private static readonly Guid UpdateRolePermissionId = Guid.Parse("66666666-0000-0000-0000-000000000008");
    private static readonly Guid DeleteRolePermissionId = Guid.Parse("66666666-0000-0000-0000-000000000009");

    private static readonly Guid AdminUserId = Guid.Parse("22222222-0000-0000-0000-000000000001");
    private static readonly Guid ViewerUserId = Guid.Parse("22222222-0000-0000-0000-000000000002");

    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedPermissionsAsync(db);
        await SeedRolesAsync(db);
        await SeedRolePermissionsAsync(db);
        await SeedUsersAsync(db);
    }

    private static async Task SeedPermissionsAsync(AppDbContext db)
    {
        var permissions = new[]
        {
            new Permission { Id = ManageAllPermissionId, Action = "manage", Subject = "all" },
            new Permission { Id = ReadUserPermissionId, Action = "read", Subject = "User" },
            new Permission { Id = CreateUserPermissionId, Action = "create", Subject = "User" },
            new Permission { Id = UpdateUserPermissionId, Action = "update", Subject = "User" },
            new Permission { Id = DeleteUserPermissionId, Action = "delete", Subject = "User" },
            new Permission { Id = ReadRolePermissionId, Action = "read", Subject = "Role" },
            new Permission { Id = CreateRolePermissionId, Action = "create", Subject = "Role" },
            new Permission { Id = UpdateRolePermissionId, Action = "update", Subject = "Role" },
            new Permission { Id = DeleteRolePermissionId, Action = "delete", Subject = "Role" }
        };

        foreach (var permission in permissions)
        {
            var exists = await db.Permissions.AnyAsync(p => p.Action == permission.Action && p.Subject == permission.Subject);
            if (!exists)
            {
                db.Permissions.Add(permission);
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(AppDbContext db)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == AdminRoleId))
        {
            db.Roles.Add(new Role
            {
                Id = AdminRoleId,
                Name = "Admin",
                Description = "Full access role"
            });
        }

        if (!await db.Roles.AnyAsync(r => r.Id == ViewerRoleId))
        {
            db.Roles.Add(new Role
            {
                Id = ViewerRoleId,
                Name = "Viewer",
                Description = "Read-only role"
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedRolePermissionsAsync(AppDbContext db)
    {
        var adminRole = await db.Roles.FirstAsync(r => r.Id == AdminRoleId);
        var viewerRole = await db.Roles.FirstAsync(r => r.Id == ViewerRoleId);

        var allPermissions = await db.Permissions.ToListAsync();
        var permissionLookup = allPermissions.ToDictionary(p => (p.Action, p.Subject), p => p.Id);

        var adminPermissionIds = new[]
        {
            permissionLookup[("manage", "all")]
        };

        var viewerPermissionIds = new[]
        {
            permissionLookup[("read", "User")],
            permissionLookup[("read", "Role")]
        };

        foreach (var permissionId in adminPermissionIds)
        {
            var exists = await db.RolePermissions.AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permissionId);
            if (!exists)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = adminRole.Id,
                    PermissionId = permissionId
                });
            }
        }

        foreach (var permissionId in viewerPermissionIds)
        {
            var exists = await db.RolePermissions.AnyAsync(rp => rp.RoleId == viewerRole.Id && rp.PermissionId == permissionId);
            if (!exists)
            {
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = viewerRole.Id,
                    PermissionId = permissionId
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(AppDbContext db)
    {
        if (!await db.Users.AnyAsync(u => u.Id == AdminUserId))
        {
            db.Users.Add(new User
            {
                Id = AdminUserId,
                Name = "Admin User",
                Email = "admin@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = AdminRoleId,
                IsActive = true
            });
        }

        if (!await db.Users.AnyAsync(u => u.Id == ViewerUserId))
        {
            db.Users.Add(new User
            {
                Id = ViewerUserId,
                Name = "Viewer User",
                Email = "viewer@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Viewer@123"),
                RoleId = ViewerRoleId,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();
    }
}
