public class AuditLog
{
    public Guid Id { get; set; }

    public string EntityName { get; set; } = default!;
    public Guid EntityId { get; set; }

    public string Action { get; set; } = default!; // CREATE, UPDATE, DELETE

    public string? UserId { get; set; } // who
    public string? UserName { get; set; }

    public string? Before { get; set; } // JSON
    public string? After { get; set; }  // JSON

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}