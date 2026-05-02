public record EmployeeCreatedEvent(
    Guid EmployeeId,
    string FullName,
    string Nip
) : IAuditableEvent
{
    public string EntityName => "Employee";
    public Guid EntityId => EmployeeId;
    public string Action => AuditActions.Create;

    public object? Before => null;
    public object? After => new { EmployeeId, FullName, Nip };
}

public record EmployeeUpdatedEvent(
    Guid EmployeeId,
    object BeforeData,
    object AfterData
) : IAuditableEvent
{
    public string EntityName => "Employee";
    public Guid EntityId => EmployeeId;
    public string Action => AuditActions.Update;

    public object? Before => BeforeData;
    public object? After => AfterData;
}

public record EmployeeDeletedEvent(Guid EmployeeId) : IAuditableEvent
{
    public string EntityName => "Employee";
    public Guid EntityId => EmployeeId;
    public string Action => AuditActions.Delete;

    public object? Before => new { EmployeeId };
    public object? After => null;
}