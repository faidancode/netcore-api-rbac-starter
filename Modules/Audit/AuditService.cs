using System.Text.Json;
using netcore_api_rbac_starter.Data;
using netcore_api_rbac_starter.Security;

public class AuditService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService? _currentUser;

    public AuditService(AppDbContext db, ICurrentUserService? currentUser = null)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(
        string entityName,
        Guid entityId,
        string action,
        object? before,
        object? after,
        CancellationToken ct)
    {
        var log = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,

            UserId = _currentUser?.UserId.ToString(),

            Before = before != null ? JsonSerializer.Serialize(before) : null,
            After = after != null ? JsonSerializer.Serialize(after) : null
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}