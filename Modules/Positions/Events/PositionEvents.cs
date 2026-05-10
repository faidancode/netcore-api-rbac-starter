public record PositionCreatedEvent(
    Guid PositionId,
    string Name,
    string? Description,
    Guid DepartmentId,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Position";
    public Guid EntityId => PositionId;
    public string Action => AuditActions.Create;
    public string? RequestId => requestId;

    public object? Before => null;
    public object? After => new { PositionId, Name, Description, DepartmentId };
}

public record PositionUpdatedEvent(
    Guid PositionId,
    object BeforeData,
    object AfterData,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Position";
    public Guid EntityId => PositionId;
    public string Action => AuditActions.Update;
    public string? RequestId => requestId;

    public object? Before => BeforeData;
    public object? After => AfterData;
}

public record PositionDeletedEvent(
    Guid PositionId,
    object BeforeData,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Position";
    public Guid EntityId => PositionId;
    public string Action => AuditActions.Delete;
    public string? RequestId => requestId;

    public object? Before => BeforeData;
    public object? After => null;
}
