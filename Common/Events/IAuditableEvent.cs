public interface IAuditableEvent
{
    string EntityName { get; }
    Guid EntityId { get; }
    string Action { get; }
    string? RequestId { get; }

    object? Before { get; }
    object? After { get; }
}
