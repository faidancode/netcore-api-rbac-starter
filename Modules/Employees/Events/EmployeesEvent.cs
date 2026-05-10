public record EmployeeCreatedEvent(
    Guid EmployeeId,
    string FullName,
    string Nip,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Employee";
    public Guid EntityId => EmployeeId;
    public string Action => AuditActions.Create;
    public string? RequestId => requestId;

    public object? Before => null;
    public object? After => new { EmployeeId, FullName, Nip };
}

public record EmployeeUpdatedEvent(
    Guid EmployeeId,
    object BeforeData,
    object AfterData,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "Employee";
    public Guid EntityId => EmployeeId;
    public string Action => AuditActions.Update;
    public string? RequestId => requestId;

    public object? Before => BeforeData;
    public object? After => AfterData;
}

public record EmployeeDeletedEvent(Guid EmployeeId, string? requestId = null) : IAuditableEvent
{
    public string EntityName => "Employee";
    public Guid EntityId => EmployeeId;
    public string Action => AuditActions.Delete;
    public string? RequestId => requestId;

    public object? Before => new { EmployeeId };
    public object? After => null;
}
