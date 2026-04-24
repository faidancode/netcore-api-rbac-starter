namespace netcore_api_rbac_starter.Entities;

public class Permission : BaseEntity
{
    public string Action { get; set; } = string.Empty;   // e.g. "read", "create", "update", "delete", "manage"
    public string Subject { get; set; } = string.Empty;  // e.g. "User", "Employee", "all"
    public string? Conditions { get; set; }               // JSON conditions (CASL-style)
    public string? Fields { get; set; }                   // comma-separated field restrictions

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}