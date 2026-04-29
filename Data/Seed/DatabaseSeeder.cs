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
        await SeedDepartmentsAsync(db);
        await SeedPositionsAsync(db);
        await SeedEmployeesAsync(db);
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

    private static async Task SeedDepartmentsAsync(AppDbContext db)
    {
        var departments = new[]
        {
            new Department { Name = "Engineering", Description = "Builds and maintains the product." },
            new Department { Name = "Human Resources", Description = "Handles hiring and employee support." },
            new Department { Name = "Finance", Description = "Manages accounting and finance operations." },
            new Department { Name = "Sales", Description = "Owns revenue generation and partnerships." },
            new Department { Name = "Operations", Description = "Supports day-to-day business operations." }
        };

        foreach (var department in departments)
        {
            var exists = await db.Departments.AnyAsync(d => d.Name == department.Name);
            if (!exists)
            {
                db.Departments.Add(department);
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedPositionsAsync(AppDbContext db)
    {
        var departmentIds = await db.Departments
            .Select(d => new { d.Id, d.Name })
            .ToDictionaryAsync(x => x.Name, x => x.Id);

        var positions = new[]
        {
            ("Engineering", "Software Engineer", "Develops product features and services."),
            ("Engineering", "Senior Software Engineer", "Designs and delivers complex backend work."),
            ("Engineering", "Tech Lead", "Leads technical decisions for the engineering team."),
            ("Engineering", "QA Engineer", "Ensures product quality through testing."),
            ("Engineering", "DevOps Engineer", "Maintains delivery pipelines and infrastructure."),
            ("Human Resources", "HR Generalist", "Supports daily HR operations."),
            ("Human Resources", "HR Manager", "Leads HR processes and people programs."),
            ("Finance", "Accountant", "Handles bookkeeping and reporting."),
            ("Finance", "Finance Analyst", "Supports budgeting and financial analysis."),
            ("Sales", "Sales Executive", "Manages prospects and client relationships."),
            ("Sales", "Sales Manager", "Leads the sales team and pipeline."),
            ("Operations", "Operations Specialist", "Coordinates internal operational workflows."),
            ("Operations", "Operations Manager", "Oversees business operations and execution.")
        };

        foreach (var (departmentName, positionName, description) in positions)
        {
            var departmentId = departmentIds[departmentName];
            var exists = await db.Positions.AnyAsync(p => p.DepartmentId == departmentId && p.Name == positionName);
            if (!exists)
            {
                db.Positions.Add(new Position
                {
                    Name = positionName,
                    Description = description,
                    DepartmentId = departmentId
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedEmployeesAsync(AppDbContext db)
    {
        var departments = await db.Departments
            .Select(d => new { d.Id, d.Name })
            .ToDictionaryAsync(x => x.Name, x => x.Id);

        var positions = await db.Positions
            .Select(p => new { p.Id, p.DepartmentId, p.Name })
            .ToListAsync();

        Guid GetPositionId(string departmentName, string positionName)
        {
            var departmentId = departments[departmentName];
            return positions.First(p => p.DepartmentId == departmentId && p.Name == positionName).Id;
        }

        var employees = new[]
        {
            new { FullName = "Andi Prakoso", Nip = "EMP-001", Gender = Gender.Male, Department = "Engineering", Position = "Senior Software Engineer", Joined = new DateOnly(2020, 1, 15) },
            new { FullName = "Bella Sari", Nip = "EMP-002", Gender = Gender.Female, Department = "Engineering", Position = "Software Engineer", Joined = new DateOnly(2020, 3, 10) },
            new { FullName = "Cahyo Wibowo", Nip = "EMP-003", Gender = Gender.Male, Department = "Engineering", Position = "QA Engineer", Joined = new DateOnly(2020, 5, 4) },
            new { FullName = "Dewi Kartika", Nip = "EMP-004", Gender = Gender.Female, Department = "Engineering", Position = "DevOps Engineer", Joined = new DateOnly(2020, 7, 20) },
            new { FullName = "Eko Nugroho", Nip = "EMP-005", Gender = Gender.Male, Department = "Engineering", Position = "Tech Lead", Joined = new DateOnly(2019, 11, 12) },
            new { FullName = "Fitri Lestari", Nip = "EMP-006", Gender = Gender.Female, Department = "Engineering", Position = "Software Engineer", Joined = new DateOnly(2021, 2, 8) },
            new { FullName = "Gilang Mahendra", Nip = "EMP-007", Gender = Gender.Male, Department = "Engineering", Position = "Senior Software Engineer", Joined = new DateOnly(2021, 4, 18) },
            new { FullName = "Hana Putri", Nip = "EMP-008", Gender = Gender.Female, Department = "Engineering", Position = "QA Engineer", Joined = new DateOnly(2021, 8, 23) },
            new { FullName = "Intan Maharani", Nip = "EMP-009", Gender = Gender.Female, Department = "Human Resources", Position = "HR Manager", Joined = new DateOnly(2019, 9, 2) },
            new { FullName = "Joko Saputra", Nip = "EMP-010", Gender = Gender.Male, Department = "Human Resources", Position = "HR Generalist", Joined = new DateOnly(2020, 1, 6) },
            new { FullName = "Kiki Amelia", Nip = "EMP-011", Gender = Gender.Female, Department = "Human Resources", Position = "HR Generalist", Joined = new DateOnly(2020, 6, 11) },
            new { FullName = "Lutfi Ramadhan", Nip = "EMP-012", Gender = Gender.Male, Department = "Finance", Position = "Finance Analyst", Joined = new DateOnly(2020, 2, 14) },
            new { FullName = "Maya Laksmi", Nip = "EMP-013", Gender = Gender.Female, Department = "Finance", Position = "Accountant", Joined = new DateOnly(2020, 4, 9) },
            new { FullName = "Nanda Pratama", Nip = "EMP-014", Gender = Gender.Male, Department = "Finance", Position = "Accountant", Joined = new DateOnly(2021, 1, 19) },
            new { FullName = "Oktavia Siregar", Nip = "EMP-015", Gender = Gender.Female, Department = "Finance", Position = "Finance Analyst", Joined = new DateOnly(2021, 5, 28) },
            new { FullName = "Putra Wijaya", Nip = "EMP-016", Gender = Gender.Male, Department = "Sales", Position = "Sales Manager", Joined = new DateOnly(2019, 10, 7) },
            new { FullName = "Qori Azzahra", Nip = "EMP-017", Gender = Gender.Female, Department = "Sales", Position = "Sales Executive", Joined = new DateOnly(2020, 2, 26) },
            new { FullName = "Rizky Firmansyah", Nip = "EMP-018", Gender = Gender.Male, Department = "Sales", Position = "Sales Executive", Joined = new DateOnly(2020, 8, 14) },
            new { FullName = "Sinta Dewi", Nip = "EMP-019", Gender = Gender.Female, Department = "Sales", Position = "Sales Executive", Joined = new DateOnly(2021, 3, 3) },
            new { FullName = "Taufik Akbar", Nip = "EMP-020", Gender = Gender.Male, Department = "Sales", Position = "Sales Executive", Joined = new DateOnly(2021, 9, 17) },
            new { FullName = "Ulfah Kirana", Nip = "EMP-021", Gender = Gender.Female, Department = "Operations", Position = "Operations Manager", Joined = new DateOnly(2019, 12, 4) },
            new { FullName = "Vino Adriansyah", Nip = "EMP-022", Gender = Gender.Male, Department = "Operations", Position = "Operations Specialist", Joined = new DateOnly(2020, 3, 30) },
            new { FullName = "Wulan Puspita", Nip = "EMP-023", Gender = Gender.Female, Department = "Operations", Position = "Operations Specialist", Joined = new DateOnly(2020, 11, 25) },
            new { FullName = "Xavier Mahendra", Nip = "EMP-024", Gender = Gender.Male, Department = "Operations", Position = "Operations Specialist", Joined = new DateOnly(2021, 6, 7) },
            new { FullName = "Yuni Handayani", Nip = "EMP-025", Gender = Gender.Female, Department = "Operations", Position = "Operations Specialist", Joined = new DateOnly(2022, 1, 13) }
        };

        foreach (var employee in employees)
        {
            var exists = await db.Employees.AnyAsync(e => e.Nip == employee.Nip);
            if (!exists)
            {
                db.Employees.Add(new Employee
                {
                    FullName = employee.FullName,
                    Nip = employee.Nip,
                    Gender = employee.Gender,
                    EmployeeStatus = EmployeeStatus.Active,
                    IsActive = true,
                    DepartmentId = departments[employee.Department],
                    PositionId = GetPositionId(employee.Department, employee.Position),
                    DateOfJoining = employee.Joined,
                    DateOfActivePosition = employee.Joined
                });
            }
        }

        await db.SaveChangesAsync();

        var seededEmployees = await db.Employees
            .Where(e => e.Nip.StartsWith("EMP-"))
            .ToListAsync();

        foreach (var employee in seededEmployees)
        {
            var hasHistory = await db.PositionHistories.AnyAsync(ph => ph.EmployeeId == employee.Id);
            if (!hasHistory)
            {
                db.PositionHistories.Add(new PositionHistory
                {
                    EmployeeId = employee.Id,
                    PositionId = employee.PositionId,
                    StartDate = employee.DateOfActivePosition ?? employee.DateOfJoining,
                    IsActive = true,
                    Notes = "Initial seeded position"
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
