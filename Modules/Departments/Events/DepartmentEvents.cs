public record DepartmentCreatedEvent(
    Guid DepartmentId,
    string Name,
    string? Description,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Department";
    public Guid EntityId => DepartmentId;
    public string Action => AuditActions.Create;
    public string? RequestId => requestId;

    public object? Before => null;
    public object? After => new { DepartmentId, Name, Description };
}

public record DepartmentUpdatedEvent(
    Guid DepartmentId,
    object BeforeData,
    object AfterData,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Department";
    public Guid EntityId => DepartmentId;
    public string Action => AuditActions.Update;
    public string? RequestId => requestId;

    public object? Before => BeforeData;
    public object? After => AfterData;
}

public record DepartmentDeletedEvent(
    Guid DepartmentId,
    object BeforeData,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Department";
    public Guid EntityId => DepartmentId;
    public string Action => AuditActions.Delete;
    public string? RequestId => requestId;

    public object? Before => BeforeData;
    public object? After => null;
}
