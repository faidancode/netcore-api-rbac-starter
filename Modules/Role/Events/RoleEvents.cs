public record RoleCreatedEvent(
    Guid RoleId,
    string Name,
    string? Description,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Role";
    public Guid EntityId => RoleId;
    public string Action => AuditActions.Create;
    public string? RequestId => requestId;

    public object? Before => null;
    public object? After => new { RoleId, Name, Description };
}

public record RoleUpdatedEvent(
    Guid RoleId,
    object BeforeData,
    object AfterData,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Role";
    public Guid EntityId => RoleId;
    public string Action => AuditActions.Update;
    public string? RequestId => requestId;

    public object? Before => BeforeData;
    public object? After => AfterData;
}

public record RolePermissionsAssignedEvent(
    Guid RoleId,
    object BeforeData,
    object AfterData,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Role";
    public Guid EntityId => RoleId;
    public string Action => AuditActions.AssignPermissions;
    public string? RequestId => requestId;

    public object? Before => BeforeData;
    public object? After => AfterData;
}

public record RoleDeletedEvent(
    Guid RoleId,
    object BeforeData,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Role";
    public Guid EntityId => RoleId;
    public string Action => AuditActions.Delete;
    public string? RequestId => requestId;

    public object? Before => BeforeData;
    public object? After => null;
}
